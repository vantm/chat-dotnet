using System.Diagnostics;
using System.Security.Claims;
using chat_dotnet.Services;

namespace chat_dotnet.Authentication;

public class ChatAuthenticationHelper(IJwtHelper jwtHelper)
{
    public (ClaimsPrincipal? User, string? Error) GetPrinciple(string token, string secretKey)
    {

        if (!jwtHelper.TryDeserialize(token, secretKey, out var jwtClaims))
        {
            return (null, "Invalid Token");
        }

        List<Claim> claims = [];

        foreach (var jwtClaim in jwtClaims)
        {
            if (jwtClaim.Key == "sub")
            {
                Debug.Assert(jwtClaim.Value is string, "JWT claim sub is string");
                claims.Add(new Claim(ClaimTypes.NameIdentifier, jwtClaim.Value.ToString()!));
            }
            else if (jwtClaim.Key == "exp")
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtClaim.Value.ToString()!));
                if (expiresAt < DateTimeOffset.UtcNow)
                {
                    return (null, "Token expired");
                }
            }
        }

        var identity = new ClaimsIdentity(claims, "Chat");
        var user = new ClaimsPrincipal(identity);
        return (user, null);
    }
}


