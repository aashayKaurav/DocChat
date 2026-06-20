using DocChat.Application;
using DocChat.Infrastructure;
using DocChat.API.Endpoints;
using DocChat.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS - allow React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddAntiforgery();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();

app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapDocumentEndpoints();

app.MapChatEndpoints();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();