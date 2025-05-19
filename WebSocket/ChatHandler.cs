using System.Net.WebSockets;
using System.Security.Claims;
using chat_dotnet.Authentication;
using chat_dotnet.Models;
using chat_dotnet.Services;

namespace chat_dotnet;

public class ChatHandler(ChatManager chat, ChatAuthenticationHelper helper, IMessageSerializer serializer, ILogger<ChatHandler> logger)
{
    private WebSocket ws = default!;
    private ClaimsPrincipal user = default!;

    public bool IsAuthenticated => user is not null;

    public void Inject(WebSocket ws)
    {
        this.ws = ws;
    }

    public async Task SayWelcomeAsync(CancellationToken cancellationToken)
    {
        await SendMessageAsync("connected", "You are connected to the server.", cancellationToken);
    }

    public async Task HandleAuthenticationAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var message = serializer.Deserialize(buffer);
        if (message.Type != "authenticate")
        {
            await SendMessageAsync("error", "Invalid authentication message.", cancellationToken);
            return;
        }

        var token = message.Payload;
        var (authenticatedUser, sessionId, error) = helper.GetPrinciple(token, "dotnet");
        if (error is not null)
        {
            await SendMessageAsync("unauthenticated", error, cancellationToken);
            return;
        }


        // TODO: check session

        user = authenticatedUser!;
        await SendMessageAsync("authenticated", "You are authenticated.", cancellationToken);
    }

    public async Task HandleRequestAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var message = serializer.Deserialize(buffer);

        logger.LogInformation("Received message: {Type} - {Payload}", message.Type, message.Payload);
    }

    public async Task SendMessageAsync(string type, string message, CancellationToken cancellationToken)
    {
        var msg = new Message(type, message);
        var buffer = serializer.Serialize(msg);
        var segment = new ArraySegment<byte>(buffer);
        await ws.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
    }
}
