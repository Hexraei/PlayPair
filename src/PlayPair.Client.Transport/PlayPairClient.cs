using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using PlayPair.Client.Sync.Abstractions;
using PlayPair.Contracts.Models;
using System.Text.Json;

namespace PlayPair.Client.Transport;

public sealed class PlayPairClient : IRoomSyncTransport, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<PlayPairClient> _logger;

    public event EventHandler<RoomCommandReceivedEventArgs>? CommandReceived;
    public event EventHandler<RoomSnapshotReceivedEventArgs>? SnapshotReceived;
    public event EventHandler<string>? Disconnected;

    public string? ClientId { get; private set; }

    public PlayPairClient(string serverUrl, ILogger<PlayPairClient> logger)
    {
        _logger = logger;
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            Disconnected?.Invoke(this, error?.Message ?? "Connection closed");
        };

        _connection.On<RoomCommand>("CommandRelayed", command =>
        {
            CommandReceived?.Invoke(this, new RoomCommandReceivedEventArgs(command));
        });

        _connection.On<RoomSnapshot>("RoomSnapshotUpdated", snapshot =>
        {
            SnapshotReceived?.Invoke(this, new RoomSnapshotReceivedEventArgs(snapshot));
        });
    }

    public async Task<RoomJoinResponse> CreateRoomAsync(string displayName, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        var response = await _connection.InvokeAsync<RoomJoinResponse>("CreateRoom", displayName, cancellationToken);
        ClientId = response.ClientId;
        return response;
    }

    public async Task<RoomJoinResponse> JoinRoomAsync(string roomCode, string displayName, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        var response = await _connection.InvokeAsync<RoomJoinResponse>("JoinRoom", roomCode, displayName, cancellationToken);
        ClientId = response.ClientId;
        return response;
    }

    public async Task<RoomJoinResponse> ReconnectRoomAsync(string roomCode, string clientId, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        var response = await _connection.InvokeAsync<RoomJoinResponse>("ReconnectRoom", roomCode, clientId, cancellationToken);
        ClientId = response.ClientId;
        return response;
    }

    public async Task LeaveRoomAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveRoom", cancellationToken);
        }
        ClientId = null;
    }

    public async Task SendCommandAsync(RoomCommand command, CancellationToken cancellationToken)
    {
        var result = await _connection.InvokeAsync<CommandSubmissionResponse>("SendCommand", command, cancellationToken);
        if (result.Status == "rejected")
        {
            _logger.LogWarning("Command rejected: {Reason}", result.RejectionReason);
        }
    }

    public async Task<RoomSnapshot> RequestSnapshotAsync(string roomId, CancellationToken cancellationToken)
    {
        if (ClientId == null)
        {
            throw new InvalidOperationException("Cannot request snapshot before joining a room.");
        }
        return await _connection.InvokeAsync<RoomSnapshot>("RecoverState", roomId, ClientId, cancellationToken);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

public sealed record RoomJoinResponse(
    string RoomCode,
    string ClientId,
    SourceRole AssignedRole,
    RoomSnapshot Snapshot);

public sealed record CommandSubmissionResponse(
    string Status,
    string RoomCode,
    string CommandId,
    string? RejectionReason);
