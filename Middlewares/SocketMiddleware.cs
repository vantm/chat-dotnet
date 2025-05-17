
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace chat_dotnet.Middlewares;

public class ChatWebSocket(IHostApplicationLifetime lifetime) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path != "/ws")
        {
            await next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        using var hostApplicationStoppingTokenSource = new CancellationTokenSource();
        var handler = context.RequestServices.GetRequiredService<ChatHandler>();

        handler.Inject(webSocket);

        await handler.SayWelcomeAsync(hostApplicationStoppingTokenSource.Token);

        var buffer = new byte[1024 * 8]; // 8KB buffer

        using var registration = lifetime.ApplicationStopping.Register(async () =>
        {
            if (!hostApplicationStoppingTokenSource.IsCancellationRequested)
            {
                hostApplicationStoppingTokenSource.Cancel();
            }

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
            }
        });

        while (webSocket.State == WebSocketState.Open)
        {
            Array.Clear(buffer);
            var receivedResult = await webSocket.ReceiveAsync(buffer, hostApplicationStoppingTokenSource.Token);
            if (receivedResult.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }
            else if (receivedResult.MessageType == WebSocketMessageType.Text)
            {
                await handler.SendMessageAsync("error", "Text messages are not supported.", hostApplicationStoppingTokenSource.Token);
            }
            else if (receivedResult.MessageType == WebSocketMessageType.Binary)
            {
                if (!handler.IsAuthenticated)
                {
                    await handler.HandleAuthenticationAsync(buffer, hostApplicationStoppingTokenSource.Token);
                    if (!handler.IsAuthenticated)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unauthenticated", CancellationToken.None);
                        break;
                    }
                    continue;
                }
                await handler.HandleRequestAsync(buffer, hostApplicationStoppingTokenSource.Token);
            }
        }
    }
}