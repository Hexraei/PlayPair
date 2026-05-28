using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using PlayPair.Server.Hubs;
using PlayPair.Server.Observability;
using PlayPair.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IRoomManager, InMemoryRoomManager>();
builder.Services.AddSingleton<IServerTelemetry, ServerTelemetry>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("SignalRLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Connection.Id,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.UseRateLimiter();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/health", () => Results.Ok(new { status = "ok", checks = new[] { "/health/live", "/health/ready" } }));
app.MapHub<RoomHub>("/hubs/room").RequireRateLimiting("SignalRLimit");

app.Run();

public partial class Program
{
}
