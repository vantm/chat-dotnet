using System.Diagnostics;
using Akka.Actor;
using chat_dotnet.Messaging;
using chat_dotnet.Services;

namespace chat_dotnet.Actors;

public class LoginManager : ReceiveActor
{
    public static Props Props(IJwtHelper jwtHelper) =>
        Akka.Actor.Props.Create(() => new LoginManager(jwtHelper));

    private readonly IJwtHelper _jwtHelper;

    public LoginManager(IJwtHelper jwtHelper)
    {
        _jwtHelper = jwtHelper;

        Receive(async (LoginRequest req) =>
        {
            Debug.Assert(req is not null, "Request must be provided");

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

            var sessionId = $"{userId}-{context.System.Scheduler.Now.ToUnixTimeMilliseconds()}";
            var sessionActor = context.ActorOf(LoginSession.Props(userId), sessionId);
            var expiresAt = context.System.Scheduler.Now.AddHours(1);

            sessionActor.Tell(("start-session", expiresAt));

            var claims = new Dictionary<string, object>
            {
                ["sub"] = userId,
                ["exp"] = expiresAt.ToUnixTimeSeconds(),
                ["sid"] = sessionId
            };

            var accessToken = _jwtHelper.Serialize(claims, "dotnet");

            sender.Tell(LoginResponse.Succ(accessToken));
        });
    }
}
