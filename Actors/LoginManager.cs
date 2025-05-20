using Akka.Actor;
using Akka.Event;
using chat_dotnet.Messaging;
using chat_dotnet.Services;

namespace chat_dotnet.Actors;

public class LoginManager : ReceiveActor
{
    public static Props Props(IJwtHelper jwtHelper) =>
        Akka.Actor.Props.Create(() => new LoginManager(jwtHelper));

    private readonly IJwtHelper _jwtHelper;
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    public LoginManager(IJwtHelper jwtHelper)
    {
        _jwtHelper = jwtHelper;

        Receive<(string Action, string SessionId)>((msg) => msg.Action == "check-session", (msg) =>
        {
            var sessionActor = Context.Child(msg.SessionId);
            if (!sessionActor.IsNobody())
            {
                sessionActor.Tell("noop");
            }

            _logger.Debug("check-session: IsNobody = {0}", sessionActor.IsNobody());

            Sender.Tell(!sessionActor.IsNobody());
        });

        Receive(async (LoginRequest req) =>
        {
            var context = Context;
            var sender = Context.Sender;

            var (userId, password) = req;

            var userManager = Context.ActorSelection("//user/user-manager");

            var passwordVerificationRequest = new PasswordVerificationRequest(userId, password);
            var passwordVerificationResponse = await userManager.Ask<PasswordVerificationResponse>(
                passwordVerificationRequest, TimeSpan.FromSeconds(3));
            if (!passwordVerificationResponse.IsCorrected)
            {
                sender.Tell(LoginResponse.Fail("Invalid user or password."));
                return;
            }

            var sessionId = Guid.NewGuid();
            var sessionActor = context.ActorOf(LoginSession.Props(sessionId, userId), sessionId.ToString());
            var expiresAt = context.System.Scheduler.Now.AddHours(1);

            sessionActor.Tell(("start-session", expiresAt));

            var claims = new Dictionary<string, object>
            {
                ["sub"] = userId,
                ["exp"] = expiresAt.ToUnixTimeSeconds(),
                ["sid"] = sessionId.ToString()
            };

            var accessToken = _jwtHelper.Serialize(claims, "dotnet");

            sender.Tell(LoginResponse.Succ(accessToken));
        });
    }
}
