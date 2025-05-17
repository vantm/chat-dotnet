using System.Security.Claims;
using NeatApi.Routing;

namespace chat_dotnet.API;

public class UserModule() : PathBaseRoutingModule("/users")
{
    protected override void AddRoutes(IEndpointRouteBuilder app, IEndpointConventionBuilder convention, IRoutingModuleContext context)
    {
        app.MapGet("/me", (ClaimsPrincipal user, ChatManager chatManager) =>
        {
            return Results.Ok(new { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) });
        })
        .RequireAuthorization();

        app.MapPost("/register", (UserRegistration reg, ChatManager chatManager) =>
        {
            var user = chatManager.RegisterUser(reg.Name, reg.Password);
            return Results.Ok(new { UserId = user.Id });
        })
        .AllowAnonymous();

        app.MapPost("/login", (UserLogin login, ChatManager chatManager) =>
        {
            var session = chatManager.Login(login.UserId, login.Password);
            return Results.Ok(session);
        })
        .AllowAnonymous();
    }
}


record UserRegistration(string Name, string Password);
record UserLogin(string UserId, string Password);