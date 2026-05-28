using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlayPair.Client.Transport;

namespace PlayPair.Client.AppShell.Services;

public sealed class SignalRRoomShellService : IRoomShellService
{
    private readonly PlayPairClient _client;
    private readonly ILogger<SignalRRoomShellService> _logger;
    private ShellSessionState _state = new(null, false, "Unknown", "Unknown", false);

    public SignalRRoomShellService(PlayPairClient client, ILogger<SignalRRoomShellService> logger)
    {
        _client = client;
        _logger = logger;
        _client.Disconnected += (_, _) =>
        {
            _state = _state with { IsConnected = false };
        };
    }

    public async Task<RoomOperationResult> CreateRoomAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.CreateRoomAsync("Host", cancellationToken);
            _state = _state with
            {
                RoomCode = result.RoomCode,
                IsConnected = true,
                IsInRoom = true,
                Role = result.AssignedRole
            };
            return RoomOperationResult.Success(_state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create room");
            return RoomOperationResult.Failure(ex.Message);
        }
    }

    public async Task<RoomOperationResult> JoinRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.JoinRoomAsync(roomCode, "Guest", cancellationToken);
            _state = _state with
            {
                RoomCode = result.RoomCode,
                IsConnected = true,
                IsInRoom = true,
                Role = result.AssignedRole
            };
            return RoomOperationResult.Success(_state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room");
            return RoomOperationResult.Failure(ex.Message);
        }
    }

    public async Task<RoomOperationResult> ReconnectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_state.RoomCode) || _client.ClientId == null)
        {
            return RoomOperationResult.Failure("No active room to reconnect to.");
        }

        try
        {
            var result = await _client.ReconnectRoomAsync(_state.RoomCode, _client.ClientId, cancellationToken);
            _state = _state with { IsConnected = true, Role = result.AssignedRole };
            return RoomOperationResult.Success(_state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect");
            return RoomOperationResult.Failure(ex.Message);
        }
    }

    public async Task<RoomOperationResult> LeaveRoomAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.LeaveRoomAsync(cancellationToken);
            _state = _state with
            {
                RoomCode = null,
                IsConnected = false,
                IsInRoom = false
            };
            return RoomOperationResult.Success(_state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave room");
            return RoomOperationResult.Failure(ex.Message);
        }
    }
}
