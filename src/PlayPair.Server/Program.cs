using PlayPair.Server.Hubs;
using PlayPair.Server.Observability;
using PlayPair.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IRoomManager, InMemoryRoomManager>();
builder.Services.AddSingleton<IServerTelemetry, ServerTelemetry>();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/health", () => Results.Ok(new { status = "ok", checks = new[] { "/health/live", "/health/ready" } }));
app.MapHub<RoomHub>("/hubs/room");

app.Run();

public partial class Program
{
}
