using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Abstractions;

public interface IRoomSyncTransport
{
    event EventHandler<RoomCommandReceivedEventArgs>? CommandReceived;

    event EventHandler<RoomSnapshotReceivedEventArgs>? SnapshotReceived;

    Task SendCommandAsync(RoomCommand command, CancellationToken cancellationToken);

    Task<RoomSnapshot> RequestSnapshotAsync(string roomId, CancellationToken cancellationToken);
}

public sealed class RoomCommandReceivedEventArgs(RoomCommand command) : EventArgs
{
    public RoomCommand Command { get; } = command;
}

public sealed class RoomSnapshotReceivedEventArgs(RoomSnapshot snapshot) : EventArgs
{
    public RoomSnapshot Snapshot { get; } = snapshot;
}
