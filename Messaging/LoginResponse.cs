namespace chat_dotnet.Messaging;

public record LoginResponse(bool IsSuccess, Guid? SessionId, string? Error)
{
    public static LoginResponse Fail(string message) => new(false, null, message);
    public static LoginResponse Succ(Guid id) => new(true, id, null);
}