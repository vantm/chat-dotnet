using System.Security.Claims;
using Akka.Actor;
using Akka.Hosting;
using chat_dotnet.Actors;
using chat_dotnet.Messaging;
using NeatApi.Routing;

namespace chat_dotnet.API;

public class ChatModule() : PathBaseRoutingModule("/chat")
{
    protected override void AddRoutes(IEndpointRouteBuilder app, IEndpointConventionBuilder convention, IRoutingModuleContext context)
    {
        app.MapPost("/rooms", async (CreateRoomRequest request, ClaimsPrincipal user, IActorRegistry registry, CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var chatRoomManager = await registry.GetAsync<ChatRoomManager>(cancellationToken);
            var createRoomRequest = new CreateChatRoomRequest(request.Name, userId);
            var response = await chatRoomManager.Ask<CreateChatRoomResponse>(createRoomRequest, cancellationToken);

            return response.IsSuccess
                ? Results.Ok(new { RoomId = response.RoomId })
                : Results.BadRequest(new { Error = response.Error });
        })
        .RequireAuthorization();

        app.MapDelete("/rooms/{roomId}", async (string roomId, ClaimsPrincipal user, IActorRegistry registry, CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var chatRoomManager = await registry.GetAsync<ChatRoomManager>(cancellationToken);
            var closeRoomRequest = new CloseChatRoomRequest(roomId, userId);
            var response = await chatRoomManager.Ask<CloseChatRoomResponse>(closeRoomRequest, cancellationToken);

            return response.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { Error = response.Error });
        })
        .RequireAuthorization();
    }
}

record CreateRoomRequest(string Name);