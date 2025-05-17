using JWT.Algorithms;
using JWT.Builder;

namespace chat_dotnet.Services.Implementations;

public class HMACSHA256JwtHelper(ILogger<HMACSHA256JwtHelper> logger) : IJwtHelper
{
    private static readonly IJwtAlgorithm alg = new HMACSHA256Algorithm();

    public string Serialize(IEnumerable<KeyValuePair<string, object>> claims, string secretKey)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        return JwtBuilder.Create()
            .WithAlgorithm(alg)
            .WithSecret(secretKey)
            .AddClaims(claims)
            .Encode();
    }

    public bool TryDeserialize(string token, string secretKey, out IEnumerable<KeyValuePair<string, object>> claims)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            claims = JwtBuilder.Create()
                .WithAlgorithm(alg)
                .WithSecret(secretKey)
                .Decode<Dictionary<string, object>>(token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize JWT token.");
            claims = [];
            return false;
        }
    }
}