using Akka.Actor;

namespace chat_dotnet.Actors;

public class LoginSession : ReceiveActor
{
    public static Props Props(string userId) =>
        Akka.Actor.Props.Create(() => new LoginSession(userId));

    public string UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; } = DateTimeOffset.MinValue;

    public bool IsExpired => ExpiresAt > Context.System.Scheduler.Now;

    public LoginSession(string userId)
    {
        UserId = userId;

        Receive(
            ((string Type, DateTimeOffset ExpiresAt) command) =>
                command.Type == "start-session",
            (command) => ExpiresAt = command.ExpiresAt);
    }
}
