using MessagePack;

namespace chat_dotnet.Models;

[MessagePackObject]
public record Message(
    [property: Key(0)] string Type,
    [property: Key(1)] string Payload);