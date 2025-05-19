using System.Diagnostics;
using System.Security.Claims;
using chat_dotnet.Services;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHelper(IJwtHelper jwtHelper)
{
    public (ClaimsPrincipal? User, string? SessionId, string? Error) GetPrinciple(string token, string secretKey)
    {

        if (!jwtHelper.TryDeserialize(token, secretKey, out var jwtClaims))
        {
            return (null, null, "Invalid Token");
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
                sessionId = value.ToString();
            }
            else if (key == "exp")
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(value.ToString()!));
                if (expiresAt < DateTimeOffset.UtcNow)
                {
                    return (null, null, "Token expired");
                }
            }
        }

        var identity = new ClaimsIdentity(claims, "Chat");
        var user = new ClaimsPrincipal(identity);
        return (user, sessionId, null);
    }
}


