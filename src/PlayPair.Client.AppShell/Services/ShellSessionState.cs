namespace PlayPair.Client.AppShell.Services;

public sealed record ShellSessionState(
    string? RoomCode,
    bool IsConnected,
    string MediaDetectionStatus,
    string SyncState,
    bool IsInRoom);
