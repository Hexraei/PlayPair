using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayPair.Contracts.Models;
using PlayPair.Server.Hubs;
using PlayPair.Server.Observability;

namespace PlayPair.Server.Tests;

public class RoomHubIntegrationTests : IAsyncLifetime
{
    private readonly TestServerTelemetry _telemetry = new();
    private readonly TestWebApplicationFactory _factory;
    private readonly List<HubConnection> _connections = [];

    public RoomHubIntegrationTests()
    {
        _factory = new TestWebApplicationFactory(_telemetry);
    }

    [Fact]
    public async Task HostCommand_IsRelayed_DuplicateIgnored_GuestMutatingCommandRejected()
    {
        var hostConnection = await CreateConnectionAsync();
        var guestConnection = await CreateConnectionAsync();
        var relayedToGuest = new ConcurrentQueue<RoomCommand>();

        guestConnection.On<RoomCommand>(
            "CommandRelayed",
            command => relayedToGuest.Enqueue(command));

        var hostJoin = await hostConnection.InvokeAsync<RoomJoinResult>("CreateRoom", "Host");
        var guestJoin = await guestConnection.InvokeAsync<RoomJoinResult>("JoinRoom", hostJoin.RoomCode, "Guest");

        var hostPlay = new RoomCommand
        {
            CommandId = Guid.NewGuid().ToString("N"),
            RoomId = hostJoin.RoomCode,
            SourceClientId = hostJoin.ClientId,
            SourceRole = SourceRole.HOST,
            Type = CommandType.PLAY,
            ProtocolVersion = 1,
            EmittedAt = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.SerializeToElement(new { })
        };

        var firstSubmit = await hostConnection.InvokeAsync<CommandSubmissionResult>("SendCommand", hostPlay);
        var duplicateSubmit = await hostConnection.InvokeAsync<CommandSubmissionResult>("SendCommand", hostPlay);

        await AssertEventuallyAsync(
            () => relayedToGuest.Count == 1 && relayedToGuest.TryPeek(out var command) && command.CommandId == hostPlay.CommandId,
            TimeSpan.FromSeconds(5));

        Assert.Equal("accepted", firstSubmit.Status);
        Assert.Equal("duplicate", duplicateSubmit.Status);

        var guestPause = hostPlay with
        {
            CommandId = Guid.NewGuid().ToString("N"),
            SourceClientId = guestJoin.ClientId,
            SourceRole = SourceRole.GUEST,
            Type = CommandType.PAUSE
        };

        var error = await Assert.ThrowsAsync<HubException>(() => guestConnection.InvokeAsync<CommandSubmissionResult>("SendCommand", guestPause));
        Assert.Contains("host_required", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(_telemetry.CommandRelayCount >= 1);
        Assert.True(_telemetry.CommandDuplicateCount >= 1);
        Assert.True(_telemetry.CommandRejectedCount >= 1);
    }

    [Fact]
    public async Task OutOfOrderMutatingCommands_AreRejected()
    {
        var hostConnection = await CreateConnectionAsync();
        var hostJoin = await hostConnection.InvokeAsync<RoomJoinResult>("CreateRoom", "Host");

        var newer = new RoomCommand
        {
            CommandId = Guid.NewGuid().ToString("N"),
            RoomId = hostJoin.RoomCode,
            SourceClientId = hostJoin.ClientId,
            SourceRole = SourceRole.HOST,
            Type = CommandType.PLAY,
            ProtocolVersion = 1,
            EmittedAt = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.SerializeToElement(new { })
        };

        _ = await hostConnection.InvokeAsync<CommandSubmissionResult>("SendCommand", newer);

        var older = newer with
        {
            CommandId = Guid.NewGuid().ToString("N"),
            Type = CommandType.PAUSE,
            EmittedAt = newer.EmittedAt.AddSeconds(-2)
        };

        var error = await Assert.ThrowsAsync<HubException>(() => hostConnection.InvokeAsync<CommandSubmissionResult>("SendCommand", older));
        Assert.Contains("stale_command", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReconnectAndRecoverState_ReturnSnapshotAndIncrementMetric()
    {
        var hostConnection = await CreateConnectionAsync();
        var hostJoin = await hostConnection.InvokeAsync<RoomJoinResult>("CreateRoom", "Host");
        await hostConnection.DisposeAsync();
        _connections.Remove(hostConnection);

        var reconnectingHost = await CreateConnectionAsync();
        var reconnect = await reconnectingHost.InvokeAsync<RoomJoinResult>("ReconnectRoom", hostJoin.RoomCode, hostJoin.ClientId);
        var recoveredSnapshot = await reconnectingHost.InvokeAsync<RoomSnapshot>("RecoverState", hostJoin.RoomCode, hostJoin.ClientId);

        Assert.Equal(hostJoin.ClientId, reconnect.ClientId);
        Assert.Equal(hostJoin.ClientId, recoveredSnapshot.HostId);
        Assert.True(_telemetry.ReconnectCount >= 1);
    }

    [Fact]
    public async Task HealthEndpoints_ReturnSuccess()
    {
        using var client = _factory.CreateClient();
        using var liveResponse = await client.GetAsync("/health/live");
        using var readyResponse = await client.GetAsync("/health/ready");

        Assert.True(liveResponse.IsSuccessStatusCode);
        Assert.True(readyResponse.IsSuccessStatusCode);
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
            }
        }

        _factory.Dispose();
    }

    private async Task<HubConnection> CreateConnectionAsync()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress!, "/hubs/room"),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await connection.StartAsync();
        _connections.Add(connection);
        return connection;
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition, TimeSpan timeout)
    {
        var started = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - started < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.True(condition(), "Condition was not satisfied before timeout.");
    }

    private sealed class TestWebApplicationFactory(TestServerTelemetry telemetry) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                var existing = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(IServerTelemetry));
                if (existing is not null)
                {
                    services.Remove(existing);
                }

                services.AddSingleton<IServerTelemetry>(telemetry);
            });
        }
    }

    private sealed class TestServerTelemetry : IServerTelemetry
    {
        public int RoomJoinCount { get; private set; }
        public int RoomLeaveCount { get; private set; }
        public int CommandRelayCount { get; private set; }
        public int CommandRejectedCount { get; private set; }
        public int CommandDuplicateCount { get; private set; }
        public int ReconnectCount { get; private set; }

        public void RoomJoined() => RoomJoinCount++;

        public void RoomLeft() => RoomLeaveCount++;

        public void CommandRelayed() => CommandRelayCount++;

        public void CommandRejected(string reason) => CommandRejectedCount++;

        public void CommandDuplicate() => CommandDuplicateCount++;

        public void Reconnected() => ReconnectCount++;
    }
}
