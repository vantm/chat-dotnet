using chat_dotnet.Models;
using chat_dotnet.Services;

namespace chat_dotnet;

public class ChatManager(IJwtHelper jwtHelper)
{
    private List<User> _users { get; } = [];
    private List<Room> _rooms { get; } = [];
    private List<Session> _sessions { get; } = [];

    public User? FindUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return _users.FirstOrDefault(u => u.Id == userId);
    }

    public Room? FindRoom(string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        return _rooms.FirstOrDefault(r => r.Id == roomId);
    }

    public User RegisterUser(string name, string password)
    {
        if (password is null || password is { Length: < 4 or > 100 })
        {
            throw new InvalidOperationException("Password must be at least 4 and 50 characters long.");
        }
        if (name is null || name is { Length: < 2 or > 50 })
        {
            throw new InvalidOperationException("Name must be between 2 and 50 characters long.");
        }

        var user = User.Create(name, password);
        _users.Add(user);
        return user;
    }

    public AccessToken Login(string userId, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var user = FindUser(userId);
        if (user == null || !user.CheckPassword(password))
        {
            throw new InvalidOperationException("Invalid user or password.");
        }

        var session = Session.Create(userId, DateTimeOffset.UtcNow.AddHours(1));
        _sessions.Add(session);

        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id,
            ["exp"] = session.ExpiresAt.ToUnixTimeSeconds()
        };

        var at = jwtHelper.Serialize(claims, "dotnet");

        return new(at);
    }
}
