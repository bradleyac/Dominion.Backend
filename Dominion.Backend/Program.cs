using Dominion.Backend;
using Dominion.Backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR(opts => opts.EnableDetailedErrors = true);
builder.Services.AddCors();
builder.Services.AddSingleton<IGameStateService, InMemoryGameStateService>();
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(x => x
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials());
}

app.UseRequireGoogleIdTokenMiddleware();

app.MapHub<GameHub>("/gameHub");

app.Run();