using Akka.Actor;
using Akka.Persistence;
using chat_dotnet.Events;
using chat_dotnet.Messaging;
using chat_dotnet.Models;

namespace chat_dotnet.Actors;

public class UserManager : PersistentActor
{
    // I know I know, this is not the best way to store users, but for
    // simplicity, we will use an in-memory list.
    private readonly IList<User> _users = [];

    public override string PersistenceId => "user-manager";

    protected override bool ReceiveRecover(object message)
    {
        switch (message)
        {
            case UserRegisteredEvent evt:
                _users.Add(evt.User);
                return true;
            default:
                return false;
        }
    }

    protected override bool ReceiveCommand(object message)
    {
        switch (message)
        {
            case RegisterRequest reg:
                var (registeredUser, errors) = CreateUser(reg);
                if (registeredUser != null)
                {
                    Persist(new UserRegisteredEvent(registeredUser), evt =>
                    {
                        var response = new RegisterResponse(true, registeredUser.Id, null);

                        _users.Add(registeredUser);

                        Sender.Tell(response);
                    });
                }
                else
                {
                    var response = new RegisterResponse(false, null, errors);
                    Sender.Tell(response);
                }
                return true;

            case PasswordVerificationRequest req:
                var (userId, password) = req;
                var user = _users.FirstOrDefault(x => x.Id == userId);
                var isCorrected = false;
                if (user is not null)
                {
                    isCorrected = user.CheckPassword(password);
                }
                var verificationResponse = new PasswordVerificationResponse(isCorrected);
                Sender.Tell(verificationResponse);
                return true;

            default:
                return false;
        }
    }

    public static (User? Right, string[]? Left) CreateUser(RegisterRequest reg)
    {
        ArgumentNullException.ThrowIfNull(reg);
        var (name, password) = reg;

        IList<string> errors = [];
        if (password is null || password is { Length: < 4 or > 100 })
        {
            errors.Add("Password must be at least 4 and 50 characters long.");
        }
        if (name is null || name is { Length: < 2 or > 50 })
        {
            errors.Add("Name must be between 2 and 50 characters long.");
        }

        if (errors.Any())
        {
            return (null, [.. errors]);
        }

        var user = User.Create(name!, password!);
        return (user, null);
    }
}