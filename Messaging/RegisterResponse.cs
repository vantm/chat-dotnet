namespace chat_dotnet.Messaging;

public record RegisterResponse(bool IsSucceed, string? UserId, string[]? Errors);