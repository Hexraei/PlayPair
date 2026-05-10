namespace PlayPair.Client.AppShell.Services;

public interface IRoomShellService
{
    Task<RoomOperationResult> CreateRoomAsync(CancellationToken cancellationToken);

    Task<RoomOperationResult> JoinRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<RoomOperationResult> ReconnectAsync(CancellationToken cancellationToken);

    Task<RoomOperationResult> LeaveRoomAsync(CancellationToken cancellationToken);
}
