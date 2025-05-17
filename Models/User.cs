using System.Security.Cryptography;
using System.Text;

namespace chat_dotnet.Models;

public class User
{
    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public byte[] Password { get; private set; } = default!;

    public bool CheckPassword(string password)
    {
        var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return hashed.SequenceEqual(Password);
    }

    public static User Create(string name, string password)
    {
        var id = Guid.NewGuid().ToString("N");
        return new User
        {
            Id = id,
            Name = name,
            Password = SHA256.HashData(Encoding.UTF8.GetBytes(password))
        };
    }
}
