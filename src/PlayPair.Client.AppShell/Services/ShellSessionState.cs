using PlayPair.Contracts.Models;

namespace PlayPair.Client.AppShell.Services;

public sealed record ShellSessionState(
    string? RoomCode,
    bool IsConnected,
    string MediaDetectionStatus,
    string SyncState,
    bool IsInRoom,
    SourceRole Role = SourceRole.GUEST);

