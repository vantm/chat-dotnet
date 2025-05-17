namespace chat_dotnet.Models;

public class Session
{
    public string Id { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; } = default!;

    public static Session Create(string userId, DateTimeOffset expiresAt)
    {
        return new Session
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ExpiresAt = expiresAt,
        };
    }
}