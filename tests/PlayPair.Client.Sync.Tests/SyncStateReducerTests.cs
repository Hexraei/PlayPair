using System.Text.Json;
using PlayPair.Client.Sync.Reducer;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Tests;

public sealed class SyncStateReducerTests
{
    private static readonly SyncEngineOptions Options = new();

    [Fact]
    public void ReduceLocalObservation_HostPublishesPlayWhenChanged()
    {
        var state = new SyncReducerState(new SyncSessionContext("ROOM01", "host-1", SourceRole.HOST));
        var observed = CreateObserved(PlaybackStatus.PLAYING, 1_000);

        var result = SyncStateReducer.ReduceLocalObservation(state, observed, Options);

        Assert.True(result.ShouldPublishLocalCommand);
        Assert.Equal(SyncOperation.Play, result.LocalCommandOperation);
    }

    [Fact]
    public void ReduceLocalObservation_GuestNeverPublishesMutatingCommand()
    {
        var state = new SyncReducerState(new SyncSessionContext("ROOM01", "guest-1", SourceRole.GUEST));
        var observed = CreateObserved(PlaybackStatus.PAUSED, 1_000);

        var result = SyncStateReducer.ReduceLocalObservation(state, observed, Options);

        Assert.False(result.ShouldPublishLocalCommand);
        Assert.Equal("local_guest_not_authoritative", result.Reason);
    }

    [Fact]
    public void ReduceRemoteCommand_DedupesCommandId()
    {
        var state = new SyncReducerState(
            new SyncSessionContext("ROOM01", "guest-1", SourceRole.GUEST),
            KnownCommandIds: ["cmd-1"]);

        var result = SyncStateReducer.ReduceRemoteCommand(state, CreateCommand("cmd-1", CommandType.PLAY, SourceRole.HOST), Options);

        Assert.False(result.ShouldApplyRemoteIntent);
        Assert.Equal("remote_deduped_command_id", result.Reason);
    }

    [Fact]
    public void ReduceRemoteCommand_RejectsGuestMutatingCommand()
    {
        var state = new SyncReducerState(new SyncSessionContext("ROOM01", "guest-2", SourceRole.GUEST));
        var command = CreateCommand("cmd-2", CommandType.PAUSE, SourceRole.GUEST);

        var result = SyncStateReducer.ReduceRemoteCommand(state, command, Options);

        Assert.False(result.ShouldApplyRemoteIntent);
        Assert.True(result.ShouldRequestSnapshot);
        Assert.Equal("remote_non_host_mutation", result.Reason);
    }

    [Fact]
    public void ReduceRemoteCommand_OutOfOrderRequestsResync()
    {
        var state = new SyncReducerState(
            new SyncSessionContext("ROOM01", "guest-2", SourceRole.GUEST),
            LastRemoteCommandAt: DateTimeOffset.UtcNow);

        var command = CreateCommand(
            "cmd-3",
            CommandType.PLAY,
            SourceRole.HOST,
            emittedAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = SyncStateReducer.ReduceRemoteCommand(state, command, Options);

        Assert.True(result.ShouldRequestSnapshot);
        Assert.Equal("remote_out_of_order", result.Reason);
    }

    [Fact]
    public void ReduceRemoteCommand_ResyncRequestTriggersSnapshotRequest()
    {
        var state = new SyncReducerState(new SyncSessionContext("ROOM01", "guest-2", SourceRole.GUEST));
        var command = CreateCommand("cmd-4", CommandType.RESYNC_REQUEST, SourceRole.HOST);

        var result = SyncStateReducer.ReduceRemoteCommand(state, command, Options);

        Assert.True(result.ShouldRequestSnapshot);
        Assert.Equal("remote_resync_request", result.Reason);
    }

    [Fact]
    public void ReduceSnapshot_GuestMismatchAppliesSnapshot()
    {
        var state = new SyncReducerState(
            new SyncSessionContext("ROOM01", "guest-2", SourceRole.GUEST),
            LastLocalState: CreateObserved(PlaybackStatus.PLAYING, 100));
        var snapshot = new RoomSnapshot
        {
            RoomId = "ROOM01",
            HostId = "host-1",
            Playback = new PlaybackSnapshot { Status = PlaybackStatus.PAUSED, PositionMs = 10_000 },
            Revision = 1
        };

        var result = SyncStateReducer.ReduceSnapshot(state, snapshot, Options);

        Assert.True(result.ShouldApplySnapshot);
        Assert.Equal("snapshot_apply_guest_reconcile", result.Reason);
    }

    [Fact]
    public void ReduceSnapshot_StaleRevisionIgnored()
    {
        var state = new SyncReducerState(
            new SyncSessionContext("ROOM01", "guest-2", SourceRole.GUEST),
            LastSnapshotRevision: 10);
        var snapshot = new RoomSnapshot { RoomId = "ROOM01", HostId = "host-1", Revision = 9 };

        var result = SyncStateReducer.ReduceSnapshot(state, snapshot, Options);

        Assert.False(result.ShouldApplySnapshot);
        Assert.Equal("snapshot_stale_revision", result.Reason);
    }

    private static SyncObservedState CreateObserved(PlaybackStatus status, long positionMs)
        => new(status, positionMs, "spotify", "Track", DateTimeOffset.UtcNow);

    private static RoomCommand CreateCommand(
        string commandId,
        CommandType type,
        SourceRole sourceRole,
        DateTimeOffset? emittedAt = null)
        => new()
        {
            CommandId = commandId,
            RoomId = "ROOM01",
            SourceClientId = sourceRole == SourceRole.HOST ? "host-1" : "guest-1",
            SourceRole = sourceRole,
            Type = type,
            EmittedAt = emittedAt ?? DateTimeOffset.UtcNow,
            Payload = JsonSerializer.SerializeToElement(new { positionMs = 1234L })
        };
}
