using Akka.Actor;
using Akka.Persistence;

namespace chat_dotnet.Actors;

public class LoginSession : ReceiveActor
{
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
