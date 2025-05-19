using Akka.Actor;
using chat_dotnet.Messaging;
using chat_dotnet.Models;

namespace chat_dotnet.Actors;

public class UserManager : ReceiveActor
{
    private readonly IList<User> _users = [];

    public UserManager()
    {
        Receive<RegisterRequest>(reg =>
        {
            var (registeredUser, errors) = RegisterUser(reg);
            var response = registeredUser is not null
                ? new RegisterResponse(true, registeredUser.Id, null)
                : new RegisterResponse(false, null, errors);

            Sender.Tell(response);
        });

        Receive<PasswordVerificationRequest>(req =>
        {
            var (userId, password) = req;
            var user = _users.FirstOrDefault(x => x.Id == userId);
            var isCorrected = false;
            if (user is not null)
            {
                isCorrected = user.CheckPassword(password);
            }
            var response = new PasswordVerificationResponse(isCorrected);
            Sender.Tell(response);
        });
    }

    public (User? Right, string[]? Left) RegisterUser(RegisterRequest reg)
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

        _users.Add(user);

        return (user, null);
    }
}