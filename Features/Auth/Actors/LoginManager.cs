using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using chat_dotnet.Messaging;

namespace chat_dotnet.Actors;

public record NewLoginEvent(Guid SessionId, string UserId);

public class LoginManager : ReceivePersistentActor
{
    public static Props Props(IServiceProvider serviceProvider) =>
        Akka.Actor.Props.Create(() => new LoginManager(serviceProvider));

    public override string PersistenceId => "login-manager";

    public LoginManager(IServiceProvider serviceProvider)
    {

        Command<(string Action, string SessionId)>(shouldHandle: (msg) => msg.Action == "check-session", (msg) =>
        {
            var sessionActor = Context.Child(msg.SessionId);
            var logger = Context.GetLogger();

            logger.Debug("check-session: IsNobody = {0}", sessionActor.IsNobody());

            Sender.Tell(!sessionActor.IsNobody());
        });

        Command<LoginRequest>(async (req) =>
        {
            var context = Context;
            var sender = Context.Sender;
            var self = Context.Self;

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
            var sessionActor = context.ActorOf(LoginSession.Props(sessionId, userId, serviceProvider), sessionId.ToString());

            _ = await sessionActor.Ask<Done>("ready");

            sender.Tell(LoginResponse.Succ(sessionId));
            self.Tell(new NewLoginEvent(sessionId, userId));
        });

        Command<NewLoginEvent>(evt =>
        {
            var logger = Context.GetLogger();
            Persist(evt, x =>
            {
                logger.Info("NewLoginEvent(sessionId:{0}) had been persisted", x.SessionId);
            });
        });

        Recover<RecoveryCompleted>(_ =>
        {
            var logger = Context.GetLogger();
            logger.Debug("LoginManager recovered");
        });

        Recover<NewLoginEvent>((evt) =>
        {
            var (sessionId, userId) = evt;
            Context.ActorOf(LoginSession.Props(sessionId, userId, serviceProvider), sessionId.ToString());
        });
    }
}
