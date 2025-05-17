namespace chat_dotnet.Services;

public interface IJwtHelper
{
    string Serialize(IEnumerable<KeyValuePair<string, object>> claims, string secretKey);
    bool TryDeserialize(string token, string secretKey, out IEnumerable<KeyValuePair<string, object>> claims);
}
