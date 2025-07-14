using System.Diagnostics;
using System.Security.Claims;
using Akka.Actor;
using chat_dotnet.Services;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHelper(IJwtHelper jwtHelper, ActorSystem system)
{
    public async Task<(ClaimsPrincipal? User, string? Error)> ParseUserAsync(string token, string secretKey)
    {
        if (!jwtHelper.TryDeserialize(token, secretKey, out var jwtClaims))
        {
            return (null, "Invalid Token");
        }

        List<Claim> claims = [];
        string? sessionId = null;

        foreach (var (key, value) in jwtClaims)
        {
            if (key == "sub")
            {
                Debug.Assert(value is string, "JWT claim sub must be a string");
                claims.Add(new Claim(ClaimTypes.NameIdentifier, value.ToString()!));
            }
            else if (key == "sid")
            {
                Debug.Assert(value is string, "JWT claim sid must be a string");
                sessionId = value.ToString()!;
                claims.Add(new Claim("sid", sessionId));
            }
            else if (key == "exp")
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(value.ToString()!));
                if (expiresAt < DateTimeOffset.UtcNow)
                {
                    return (null, "Token expired");
                }
            }
        }

        var loginManager = system.ActorSelection("//user/login-manager");

        try
        {
            var msg = ("check-session", sessionId!);
            var actorExists = await loginManager.Ask<bool>(msg, TimeSpan.FromSeconds(5));
            if (!actorExists)
            {
                return (null, "The session is not found");
            }
            var identity = new ClaimsIdentity(claims, "Chat");
            var user = new ClaimsPrincipal(identity);
            return (user, null);
        }
        catch (AskTimeoutException)
        {
            return (null, "The session is not found (timeout)");
        }


        //var loginSessionActorSelection = system.ActorSelection($"//user/login-manager/{sessionId}");
        //try
        //{
        //    var result = await loginSessionActorSelection.ResolveOne(TimeSpan.FromSeconds(3));
        //    if (result.IsNobody())
        //    {
        //        return (null, "The session is not found");
        //    }

        //    var identity = new ClaimsIdentity(claims, "Chat");
        //    var user = new ClaimsPrincipal(identity);
        //    return (user, null);
        //}
        //catch (ActorNotFoundException)
        //{
        //    return (null, "The session is not found (source: exception)");
        //}
    }
}


