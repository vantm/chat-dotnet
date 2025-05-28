using chat_dotnet.Models;

namespace chat_dotnet.Events;

public record ChatRoomCreatedEvent(Room Room);
public record ChatRoomClosedEvent(string RoomId, DateTimeOffset ClosedAt);
