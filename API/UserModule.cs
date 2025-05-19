using System.Security.Claims;
using Akka.Actor;
using Akka.Hosting;
using chat_dotnet.Actors;
using chat_dotnet.Messaging;
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

        app.MapPost("/register", async (UserRegistration reg, IActorRegistry registry, CancellationToken aborted) =>
        {
            var userManagerActor = await registry.GetAsync<UserManager>(aborted);
            var registerRequest = new RegisterRequest(reg.Name, reg.Password);
            var registerResponse = await userManagerActor.Ask<RegisterResponse>(registerRequest, aborted);
            return registerResponse.IsSucceed
                ? Results.Ok(new { registerResponse.UserId })
                : Results.BadRequest(new { registerResponse.Errors });
        })
        .AllowAnonymous();

        app.MapPost("/login", async (UserLogin login, IActorRegistry registry, CancellationToken aborted) =>
        {
            var loginManagerActor = await registry.GetAsync<LoginManager>(aborted);
            var loginRequest = new LoginRequest(login.UserId, login.Password);
            var loginResponse = await loginManagerActor.Ask<LoginResponse>(loginRequest, aborted);
            return loginResponse.IsSuccess
                ? Results.Ok(new { loginResponse.AccessToken })
                : Results.BadRequest(new { loginResponse.Error });
        })
        .AllowAnonymous();
    }
}


record UserRegistration(string Name, string Password);
record UserLogin(string UserId, string Password);