namespace chat_dotnet.Messaging;

public record CreateChatRoomResponse(bool IsSuccess, string? RoomId, string? Error)
{
    public static CreateChatRoomResponse Success(string roomId) => new(true, roomId, null);
    public static CreateChatRoomResponse Fail(string error) => new(false, null, error);
}
