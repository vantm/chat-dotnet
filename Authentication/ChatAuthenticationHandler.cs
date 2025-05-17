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
        var authHeader = Request.Headers["Authorization"].ToString();
        var authHeaderParts = authHeader.Split(' ');
        if (authHeaderParts is not
            { Length: 2 })
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var scheme = authHeaderParts[0];
        if (!scheme.Equals("chat", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));
        }


        var token = authHeaderParts[1];
        var (user, error) = authHelper.GetPrinciple(token, Options.SecretKey);
        if (error is not null)
        {
            return Task.FromResult(AuthenticateResult.Fail(error));
        }
        var ticket = new AuthenticationTicket(user!, "Chat");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}