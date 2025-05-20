using System.Text.Encodings.Web;
using Akka.Actor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHandler(
    ChatAuthenticationHelper authHelper,
    IOptionsMonitor<ChatAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ChatAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-Access-Token"].ToString();
        if (token is null || token.Length == 0)
        {
            return AuthenticateResult.Fail("Missing X-Access-Token Header");
        }

        var (user, error) = await authHelper.ParseUserAsync(token, Options.SecretKey);
        if (error is not null)
        {
            return AuthenticateResult.Fail(error);
        }

        var ticket = new AuthenticationTicket(user!, "Chat");
        return AuthenticateResult.Success(ticket);
    }
}