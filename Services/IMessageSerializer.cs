using chat_dotnet.Models;

namespace chat_dotnet.Services;

public interface IMessageSerializer
{
    byte[] Serialize(Message data);
    Message Deserialize(byte[] buffer);
}
