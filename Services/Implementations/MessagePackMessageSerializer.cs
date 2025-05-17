using chat_dotnet.Models;
using MessagePack;

namespace chat_dotnet.Services.Implementations;

public class MessagePackMessageSerializer : IMessageSerializer
{
    public Message Deserialize(byte[] buffer)
    {
        return MessagePackSerializer.Deserialize<Message>(buffer);
    }

    public byte[] Serialize(Message data)
    {

        return MessagePackSerializer.Serialize(data, options);
    }

    private static readonly MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
        .WithCompression(MessagePackCompression.Lz4Block);
}