using PlayPair.Contracts.Models;

namespace PlayPair.Server.Rooms;

public sealed record RoomJoinResult(
    string RoomCode,
    string ClientId,
    SourceRole AssignedRole,
    RoomSnapshot Snapshot);

public sealed record RoomLeaveResult(
    string RoomCode,
    bool RoomClosed,
    RoomSnapshot? Snapshot);

public sealed record CommandRelayResult(
    string RoomCode,
    RoomCommand Command,
    RoomSnapshot? Snapshot,
    CommandProcessStatus Status,
    string? RejectionReason);

public enum CommandProcessStatus
{
    Accepted = 0,
    Duplicate = 1
}
