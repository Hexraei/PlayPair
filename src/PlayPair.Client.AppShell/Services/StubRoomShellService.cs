using Microsoft.Extensions.Logging;

namespace PlayPair.Client.AppShell.Services;

public sealed class StubRoomShellService(ILogger<StubRoomShellService> logger) : IRoomShellService
{
    private readonly ILogger<StubRoomShellService> _logger = logger;
    private readonly Random _random = new();
    private ShellSessionState _state = new(
        null,
        false,
        "Media detection placeholder (not integrated)",
        "Sync state placeholder (not integrated)",
        false);

    public Task<RoomOperationResult> CreateRoomAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roomCode = GenerateRoomCode();
        _state = _state with
        {
            RoomCode = roomCode,
            IsConnected = true,
            IsInRoom = true
        };

        _logger.LogInformation("Created stub room {RoomCode}", roomCode);
        return Task.FromResult(RoomOperationResult.Success(_state));
    }

    public Task<RoomOperationResult> JoinRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _state = _state with
        {
            RoomCode = roomCode,
            IsConnected = true,
            IsInRoom = true
        };

        _logger.LogInformation("Joined stub room {RoomCode}", roomCode);
        return Task.FromResult(RoomOperationResult.Success(_state));
    }

    public Task<RoomOperationResult> ReconnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_state.IsInRoom || string.IsNullOrWhiteSpace(_state.RoomCode))
        {
            _logger.LogWarning("Reconnect requested without active room");
            return Task.FromResult(RoomOperationResult.Failure("Cannot reconnect because no room is active."));
        }

        _state = _state with
        {
            IsConnected = true
        };

        _logger.LogInformation("Reconnected to stub room {RoomCode}", _state.RoomCode);
        return Task.FromResult(RoomOperationResult.Success(_state));
    }

    public Task<RoomOperationResult> LeaveRoomAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _state = _state with
        {
            RoomCode = null,
            IsConnected = false,
            IsInRoom = false
        };

        _logger.LogInformation("Left stub room");
        return Task.FromResult(RoomOperationResult.Success(_state));
    }

    private string GenerateRoomCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> buffer = stackalloc char[6];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = alphabet[_random.Next(0, alphabet.Length)];
        }

        return new string(buffer);
    }
}
