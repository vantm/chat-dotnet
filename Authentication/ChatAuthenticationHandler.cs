using System.Text.Encodings.Web;
using Akka.Actor;
using Akka.Hosting;
using chat_dotnet.Actors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHandler(
    ChatAuthenticationHelper authHelper,
    ActorSystem system,
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

        var (user, sessionId, error) = authHelper.GetPrinciple(token, Options.SecretKey);
        if (error is not null)
        {
            return AuthenticateResult.Fail(error);
        }

        var loginSessionActorSelection = system.ActorSelection($"//user/login-manager/{sessionId}");
        try
        {
            var result = await loginSessionActorSelection.ResolveOne(TimeSpan.FromSeconds(3));
            if (result.IsNobody())
            {
                return AuthenticateResult.Fail("The session is not found");
            }

            var ticket = new AuthenticationTicket(user!, "Chat");
            return AuthenticateResult.Success(ticket);
        }
        catch (ActorNotFoundException)
        {
            return AuthenticateResult.Fail("The session is not found");
        }
    }
}