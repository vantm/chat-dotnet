using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using chat_dotnet.Events;
using LogLevel = Akka.Event.LogLevel;

namespace chat_dotnet.Actors;

public record SessionState(string UserId, DateTimeOffset ExpiresAt);

public class LoginSession : ReceivePersistentActor, IWithTimers
{
    public static Props Props(Guid sessionId, string userId) =>
        Akka.Actor.Props.Create(() => new LoginSession(sessionId, userId));

    private readonly ILoggingAdapter _logger = Context.GetLogger();

    public SessionState State { get; set; } = new(string.Empty, DateTimeOffset.MinValue);
    public ITimerScheduler Timers { get; set; } = default!;

    public override string PersistenceId { get; }

    public LoginSession(Guid sessionId, string userId)
    {
        PersistenceId = $"session-{userId}-{sessionId}";

        Command<(string Type, DateTimeOffset ExpiresAt)>(
            command => command.Type == "start-session",
            command =>
            {
                Persist(new SessionStartedEvent(command.ExpiresAt), evt =>
                {
                    State = State with { ExpiresAt = command.ExpiresAt };
                    Self.Tell("start-timer");
                    Context.System.EventStream.Publish(evt);
                });
            });

        Command<string>(str => str == "start-timer", _ =>
        {
            StartTimeoutTimer();
        });

        Command<string>(str => str == "noop", _ =>
        {
            _logger.Debug("The command 'noop' invoked");
        });

        CommandAsync<string>(str => str == "timeout", _ =>
        {
            return Context.Self.GracefulStop(TimeSpan.FromSeconds(30));
        });

        Command<RecoveryCompleted>(_ =>
        {
            Self.Tell("start-timer");

            _logger.Debug("Recover Completed");
        });

        Recover<SessionStartedEvent>(evt =>
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Recover SessionStartedEvent: {0}", JsonSerializer.Serialize(evt));
            }

            State = State with { ExpiresAt = evt.ExpiresAt };
        });

        Recover<SnapshotOffer>(snapshot =>
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Recover SnapshotOffer.Snapshot: {0}", JsonSerializer.Serialize(snapshot.Snapshot));
            }

            if (snapshot.Snapshot is SessionState state)
            {
                State = state;
            }
        });
    }

    private void StartTimeoutTimer()
    {
        var timerKey = $"{PersistenceId}-timer";

        Timers.Cancel(timerKey);

        var scheduler = Context.System.Scheduler;
        var timeout = State.ExpiresAt - scheduler.Now;

        var logId = Guid.NewGuid().ToString();

        _logger.Info(
            "Session {0} will be stopped within {1} ({2}).",
            Context.Self.Path.Name, timeout.ToString(), logId);

        Timers.StartSingleTimer(timerKey, "timeout", timeout);
    }
}
