using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHandler(
    ChatAuthenticationHelper authHelper,
    IOptionsMonitor<ChatAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ChatAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-Access-Token"].ToString();
        if (token is null || token.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Access-Token Header"));
        }

        var (user, error) = authHelper.GetPrinciple(token, Options.SecretKey);
        if (error is not null)
        {
            return Task.FromResult(AuthenticateResult.Fail(error));
        }
        var ticket = new AuthenticationTicket(user!, "Chat");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}