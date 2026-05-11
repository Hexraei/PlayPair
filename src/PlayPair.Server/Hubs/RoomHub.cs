using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PlayPair.Contracts.Models;
using PlayPair.Server.Observability;
using PlayPair.Server.Rooms;

namespace PlayPair.Server.Hubs;

public sealed class RoomHub : Hub
{
    private readonly IRoomManager _roomManager;
    private readonly IServerTelemetry _telemetry;
    private readonly ILogger<RoomHub> _logger;

    public RoomHub(IRoomManager roomManager, IServerTelemetry telemetry, ILogger<RoomHub> logger)
    {
        _roomManager = roomManager;
        _telemetry = telemetry;
        _logger = logger;
    }

    public Task<RoomJoinResult> CreateRoom(string displayName)
    {
        using var _ = BeginOperationScope("CreateRoom", correlationId: Context.ConnectionId);
        var result = ExecuteOrThrow(
            () => _roomManager.CreateRoom(Context.ConnectionId, displayName),
            "CreateRoom");

        _telemetry.RoomJoined();
        return JoinSignalRGroupAndPublishSnapshotAsync(result);
    }

    public Task<RoomJoinResult> JoinRoom(string roomCode, string displayName)
    {
        using var _ = BeginOperationScope("JoinRoom", correlationId: Context.ConnectionId, roomCode: roomCode);
        var result = ExecuteOrThrow(
            () => _roomManager.JoinRoom(Context.ConnectionId, roomCode, displayName),
            "JoinRoom");

        _telemetry.RoomJoined();
        return JoinSignalRGroupAndPublishSnapshotAsync(result);
    }

    public Task<RoomJoinResult> ReconnectRoom(string roomCode, string clientId)
    {
        using var _ = BeginOperationScope("ReconnectRoom", correlationId: Context.ConnectionId, roomCode: roomCode, clientId: clientId);
        var result = ExecuteOrThrow(
            () => _roomManager.Reconnect(Context.ConnectionId, roomCode, clientId),
            "ReconnectRoom");

        _telemetry.Reconnected();
        return JoinSignalRGroupAndPublishSnapshotAsync(result);
    }

    public Task<RoomSnapshot> RecoverState(string roomCode, string clientId)
    {
        using var _ = BeginOperationScope("RecoverState", correlationId: Context.ConnectionId, roomCode: roomCode, clientId: clientId);
        return Task.FromResult(ExecuteOrThrow(
            () => _roomManager.RecoverState(Context.ConnectionId, roomCode, clientId),
            "RecoverState"));
    }

    public async Task LeaveRoom()
    {
        using var _ = BeginOperationScope("LeaveRoom", correlationId: Context.ConnectionId);
        var result = ExecuteOrThrow(
            () => _roomManager.LeaveRoom(Context.ConnectionId),
            "LeaveRoom");

        _telemetry.RoomLeft();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, result.RoomCode);
        if (!result.RoomClosed && result.Snapshot is not null)
        {
            await Clients.Group(result.RoomCode).SendAsync("RoomSnapshotUpdated", result.Snapshot);
        }
    }

    public async Task<CommandSubmissionResult> SendCommand(RoomCommand command)
    {
        if (command is null)
        {
            throw new HubException("Command cannot be null");
        }

        using var _ = BeginOperationScope(
            "SendCommand",
            correlationId: command.CommandId ?? Context.ConnectionId,
            roomCode: command.RoomId,
            clientId: command.SourceClientId);

        CommandRelayResult relay;
        try
        {
            relay = _roomManager.ProcessCommand(Context.ConnectionId, command);
        }
        catch (RoomOperationException ex)
        {
            _telemetry.CommandRejected(ex.Code);
            throw BuildHubException(ex, "SendCommand");
        }

        if (relay.Status == CommandProcessStatus.Duplicate)
        {
            _telemetry.CommandDuplicate();
            return new CommandSubmissionResult("duplicate", relay.RoomCode, relay.Command.CommandId, null);
        }

        await Clients.Group(relay.RoomCode).SendAsync("CommandRelayed", relay.Command);
        if (relay.Snapshot is not null)
        {
            await Clients.Group(relay.RoomCode).SendAsync("RoomSnapshotUpdated", relay.Snapshot);
        }

        _telemetry.CommandRelayed();
        return new CommandSubmissionResult("accepted", relay.RoomCode, relay.Command.CommandId, null);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var leftRoom = _roomManager.MarkDisconnected(Context.ConnectionId);
        if (leftRoom is not null)
        {
            _telemetry.RoomLeft();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, leftRoom.RoomCode);
            if (!leftRoom.RoomClosed && leftRoom.Snapshot is not null)
            {
                await Clients.Group(leftRoom.RoomCode).SendAsync("RoomSnapshotUpdated", leftRoom.Snapshot);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task<RoomJoinResult> JoinSignalRGroupAndPublishSnapshotAsync(RoomJoinResult result)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomCode);
        await Clients.Group(result.RoomCode).SendAsync("RoomSnapshotUpdated", result.Snapshot);
        return result;
    }

    private IDisposable BeginOperationScope(string operationName, string correlationId, string? roomCode = null, string? clientId = null)
        => _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Operation"] = operationName,
            ["CorrelationId"] = correlationId,
            ["ConnectionId"] = Context.ConnectionId,
            ["RoomCode"] = roomCode,
            ["ClientId"] = clientId
        })!;

    private T ExecuteOrThrow<T>(Func<T> operation, string operationName) where T : notnull
    {
        try
        {
            return operation()!;
        }
        catch (RoomOperationException ex)
        {
            throw BuildHubException(ex, operationName);
        }
    }

    private HubException BuildHubException(RoomOperationException ex, string operationName)
    {
        _logger.LogWarning(
            ex,
            "Room operation failed: {OperationName} for connection {ConnectionId} ({ErrorCode})",
            operationName,
            Context.ConnectionId,
            ex.Code);

        var payload = JsonSerializer.Serialize(new HubErrorPayload(ex.Code, ex.Message, Context.ConnectionId));
        return new HubException(payload);
    }

    private sealed record HubErrorPayload(string Code, string Message, string ConnectionId);
}

public sealed record CommandSubmissionResult(
    string Status,
    string RoomCode,
    string CommandId,
    string? RejectionReason);
