using Microsoft.AspNetCore.Authentication;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationOptions : AuthenticationSchemeOptions
{
    public string SecretKey { get; set; } = string.Empty;
}