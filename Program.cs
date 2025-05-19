using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using chat_dotnet;
using chat_dotnet.Actors;
using chat_dotnet.Authentication;
using chat_dotnet.Middlewares;
using chat_dotnet.Services;
using chat_dotnet.Services.Implementations;
using LinqToDB;
using Microsoft.AspNetCore.Authentication;
using Petabridge.Cmd.Host;

var builder = WebApplication.CreateBuilder(args);

builder.AddNeatApi();

builder.Services.AddProblemDetails();

builder.Services.AddAuthentication("Chat").AddChatApiToken();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IMessageSerializer, MessagePackMessageSerializer>();
builder.Services.AddSingleton<IJwtHelper, HMACSHA256JwtHelper>();
builder.Services.AddSingleton<ChatAuthenticationHelper>();

builder.Services.AddAkka("chat-system", (akkaConfigurationBuilder, serviceProvider) =>
{
    akkaConfigurationBuilder
        .AddPetabridgeCmd(
        cmd =>
        {
        })
        .ConfigureLoggers((loggerConfigBuilder) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerConfigBuilder
                .AddDefaultLogger()
                .AddLoggerFactory(loggerFactory);
        })
        .WithSqlPersistence(
            connectionString: builder.Configuration.GetConnectionString("sqlite")!,
            ProviderName.SQLite)
        .WithActors((actorSystem, registry) =>
        {
            var chatRoomManager = actorSystem.ActorOf<ChatRoomManager>("chat-room-manager");
            registry.TryRegister<ChatRoomManager>(chatRoomManager);

            var userManager = actorSystem.ActorOf<UserManager>("user-manager");
            registry.TryRegister<UserManager>(userManager);

            var jwtHelper = serviceProvider.GetRequiredService<IJwtHelper>();
            var loginManager = actorSystem.ActorOf(Props.Create<LoginManager>(jwtHelper), "login-manager");
            registry.TryRegister<LoginManager>(loginManager);
        });
});

builder.Services.AddSingleton<ChatManager>();

builder.Services.AddSingleton<ChatWebSocket>();

builder.Services.AddScoped<ChatHandler>();

var app = builder.Build();

app.UseWebSockets();
app.UseMiddleware<ChatWebSocket>();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapNeatApi();

await app.RunAsync();