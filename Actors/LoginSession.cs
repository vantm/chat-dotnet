using System.Text.Json;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using chat_dotnet.Events;
using chat_dotnet.Services;

namespace chat_dotnet.Actors;

public enum SessionStatus { Active, LoggedOut, Expired }
public record SessionState(Guid SessionId, string UserId, DateTimeOffset ExpiresAt, SessionStatus Status, DateTimeOffset? LoggedOutAt);

public class LoginSession : ReceivePersistentActor, IWithTimers
{
    public static Props Props(Guid sessionId, string userId, IServiceProvider serviceProvider)
    {
        var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
        var expiresAt = timeProvider.GetUtcNow().AddHours(1);
        return Akka.Actor.Props.Create(() => new LoginSession(new(sessionId, userId, expiresAt, SessionStatus.Active, null), serviceProvider));
    }

    public SessionState State { get; private set; }
    public ITimerScheduler Timers { get; set; } = default!;

    public override string PersistenceId { get; }

    public LoginSession(SessionState state, IServiceProvider serviceProvider)
    {
        State = state;
        PersistenceId = $"session-{state.UserId}-{state.SessionId}";

        var logger = Context.GetLogger();

        Command<string>(str => str == "ready", _ =>
        {
            var sender = Sender;

            Persist(new SessionStartedEvent(state.SessionId, state.UserId, state.ExpiresAt), evt =>
            {
                sender.Tell(Done.Instance);
            });
        });

        Command<string>(str => str == "get-jwt", _ =>
        {
            if (State.Status != SessionStatus.Active)
            {
                throw new InvalidOperationException("The session must be active");
            }

            using var scope = serviceProvider.CreateScope();
            var jwtHelper = scope.ServiceProvider.GetRequiredService<IJwtHelper>();
            var claims = new Dictionary<string, object>()
            {
                {"sub", State.UserId },
                {"exp", State.ExpiresAt.ToUnixTimeSeconds() },
                {"sid", State.SessionId.ToString() },
            };
            var jwt = jwtHelper.Serialize(claims, "dotnet");
            Sender.Tell(jwt);
        });

        Command<string>(shouldHandle: str => str == "logout", _ =>
        {
            var loggedOutAt = Context.System.Scheduler.Now;
            State = State with { Status = SessionStatus.LoggedOut, LoggedOutAt = loggedOutAt };

            var self = Self;
            var sender = Sender;

            Persist(new SessionLoggedOutEvent(loggedOutAt), evt =>
            {
                self.Tell("stop");
                sender.Tell(Done.Instance);
            });
        });

        Command<string>(shouldHandle: str => str == "expires", _ =>
        {
            State = State with { Status = SessionStatus.Expired };

            var self = Self;
            Persist(new SessionExpiredEvent(), _ =>
            {
                self.Tell("stop");
            });
        });

        Command<string>(str => str == "stop", _ =>
        {
            Self.GracefulStop(TimeSpan.FromSeconds(30));
        });

        Recover<RecoveryCompleted>(_ =>
        {
            logger.Debug("Recover Completed");

            if (State.Status != SessionStatus.Active)
            {
                Self.Tell("stop");
            }
        });

        Recover<SessionStartedEvent>(evt =>
        {
            logger.Debug("Recover SessionStartedEvent");
            State = State with
            {
                Status = SessionStatus.Active,
                UserId = evt.UserId,
                SessionId = evt.SessionId,
                ExpiresAt = evt.ExpiresAt,
                LoggedOutAt = null
            };
        });

        Recover<SessionExpiredEvent>(evt =>
        {
            logger.Debug("Recover SessionExpiredEvent");
            State = State with { Status = SessionStatus.Expired };
        });

        Recover<SessionLoggedOutEvent>(evt =>
        {
            logger.Debug("Recover SessionLoggedOutEvent");

            State = State with { Status = SessionStatus.LoggedOut, LoggedOutAt = evt.LoggedOutAt };
        });

        Recover<SnapshotOffer>(snapshot =>
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Recover SnapshotOffer.Snapshot: {0}", JsonSerializer.Serialize(snapshot.Snapshot));
            }

            if (snapshot.Snapshot is SessionState state)
            {
                State = state;
            }
        });
    }

    protected override void PreStart()
    {
        var timerKey = $"{PersistenceId}-timer";

        Timers.Cancel(timerKey);

        var scheduler = Context.System.Scheduler;
        var timeout = State.ExpiresAt - scheduler.Now;

        var logId = Guid.NewGuid().ToString();

        var logger = Context.GetLogger();

        logger.Info(
            "Session {0} will be stopped within {1} ({2}).",
            Context.Self.Path.Name, timeout.ToString(), logId);

        Timers.StartSingleTimer(timerKey, "expires", timeout);

        base.PreStart();
    }

    protected override void PostStop()
    {
        var timerKey = $"{PersistenceId}-timer";

        Timers.Cancel(timerKey);

        base.PostStop();
    }
}
