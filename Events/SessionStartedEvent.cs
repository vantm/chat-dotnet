namespace chat_dotnet.Events;

public record SessionStartedEvent(DateTimeOffset ExpiresAt);
