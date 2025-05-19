namespace chat_dotnet.Messaging;

public record LoginResponse(bool IsSuccess, string? AccessToken, string? Error)
{
    public static LoginResponse Fail(string message) => new(false, null, message);
    public static LoginResponse Succ(string message) => new(true, message, null);
}