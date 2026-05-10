using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Reducer;

public enum SyncOrigin
{
    Local,
    Remote
}

public enum SyncOperation
{
    None,
    Play,
    Pause,
    Seek,
    PlayerChanged
}

public sealed record SyncObservedState(
    PlaybackStatus PlaybackStatus,
    long? PositionMs,
    string SourceAppId,
    string Title,
    DateTimeOffset ObservedAtUtc);

public sealed record SyncSuppression(SyncOperation Operation, long? PositionMs);

public sealed record SyncReducerState(
    SyncSessionContext Context,
    SyncObservedState? LastLocalState = null,
    DateTimeOffset? LastRemoteCommandAt = null,
    long LastSnapshotRevision = -1,
    string? LastKnownRoomHostId = null,
    IReadOnlyList<string>? KnownCommandIds = null,
    IReadOnlyList<string>? LocalCommandIdsAwaitingEcho = null,
    SyncSuppression? PendingLocalSuppression = null);

public sealed record SyncReductionDecision(
    SyncReducerState NextState,
    bool ShouldPublishLocalCommand = false,
    SyncOperation LocalCommandOperation = SyncOperation.None,
    bool ShouldApplyRemoteIntent = false,
    SyncOperation RemoteIntentOperation = SyncOperation.None,
    long? RemoteIntentSeekPositionMs = null,
    bool ShouldRequestSnapshot = false,
    bool ShouldApplySnapshot = false,
    bool ShouldLogWarning = false,
    string Reason = "none");
