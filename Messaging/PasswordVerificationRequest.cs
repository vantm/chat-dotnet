namespace chat_dotnet.Messaging;

public record PasswordVerificationRequest(string UserId, string Password);
