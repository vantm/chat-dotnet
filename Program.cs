using chat_dotnet;
using chat_dotnet.Authentication;
using chat_dotnet.Middlewares;
using chat_dotnet.Services;
using chat_dotnet.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

builder.AddNeatApi();

builder.Services.AddProblemDetails();

builder.Services.AddAuthentication("Chat")
    .AddScheme<ChatAuthenticationOptions, ChatAuthenticationHandler>("Chat", options =>
    {
        options.SecretKey = "dotnet";
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IMessageSerializer, MessagePackMessageSerializer>();
builder.Services.AddSingleton<IJwtHelper, HMACSHA256JwtHelper>();
builder.Services.AddSingleton<ChatAuthenticationHelper>();

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

app.Run();
