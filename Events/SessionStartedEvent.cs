namespace chat_dotnet.Events;

public record SessionStartedEvent(Guid SessionId, string UserId, DateTimeOffset ExpiresAt);
public record SessionExpiredEvent();
public record SessionLoggedOutEvent(DateTimeOffset LoggedOutAt);
