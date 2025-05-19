
using chat_dotnet.Authentication;

namespace Microsoft.AspNetCore.Authentication;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddChatApiToken(this AuthenticationBuilder builder)
    {
        builder.AddScheme<ChatAuthenticationOptions, ChatAuthenticationHandler>("Chat", options =>
        {
            options.SecretKey = "dotnet";
        });

        return builder;
    }
}