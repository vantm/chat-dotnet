namespace chat_dotnet.Messaging;

public record CloseChatRoomResponse(bool IsSuccess, string? Error)
{
    public static CloseChatRoomResponse Success() => new(true, null);
    public static CloseChatRoomResponse Fail(string error) => new(false, error);
}
