using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Reducer;

public static class SyncStateReducer
{
    public static SyncReductionDecision ReduceLocalObservation(
        SyncReducerState state,
        SyncObservedState observedState,
        SyncEngineOptions options)
    {
        var nextState = state with { LastLocalState = observedState };

        if (state.PendingLocalSuppression is not null && MatchesSuppression(state.PendingLocalSuppression, observedState, options))
        {
            return new SyncReductionDecision(
                nextState with { PendingLocalSuppression = null },
                Reason: "local_suppressed_remote_origin");
        }

        if (!IsHost(state.Context.Role))
        {
            return new SyncReductionDecision(nextState, Reason: "local_guest_not_authoritative");
        }

        if (state.LastLocalState is null)
        {
            return new SyncReductionDecision(
                nextState,
                ShouldPublishLocalCommand: true,
                LocalCommandOperation: MapPlaybackOperation(observedState.PlaybackStatus),
                Reason: "local_initial_publish");
        }

        var operation = SelectLocalOperation(state.LastLocalState, observedState, options);
        if (operation == SyncOperation.None)
        {
            return new SyncReductionDecision(nextState, Reason: "local_no_material_change");
        }

        return new SyncReductionDecision(
            nextState,
            ShouldPublishLocalCommand: true,
            LocalCommandOperation: operation,
            Reason: "local_publish_change");
    }

    public static SyncReductionDecision ReduceRemoteCommand(
        SyncReducerState state,
        RoomCommand command,
        SyncEngineOptions options)
    {
        var knownIds = state.KnownCommandIds ?? [];
        if (ContainsCommandId(knownIds, command.CommandId))
        {
            return new SyncReductionDecision(state, Reason: "remote_deduped_command_id");
        }

        var localPending = state.LocalCommandIdsAwaitingEcho ?? [];
        if (ContainsCommandId(localPending, command.CommandId))
        {
            return new SyncReductionDecision(
                state with
                {
                    LocalCommandIdsAwaitingEcho = RemoveCommandId(localPending, command.CommandId),
                    KnownCommandIds = AppendKnownCommandId(knownIds, command.CommandId, options.MaxKnownCommandIds)
                },
                Reason: "remote_local_echo");
        }

        var nextState = state with
        {
            KnownCommandIds = AppendKnownCommandId(knownIds, command.CommandId, options.MaxKnownCommandIds)
        };

        if (state.LastRemoteCommandAt is not null &&
            command.EmittedAt < state.LastRemoteCommandAt.Value - options.EffectiveOutOfOrderTolerance)
        {
            return new SyncReductionDecision(
                nextState,
                ShouldRequestSnapshot: true,
                ShouldLogWarning: true,
                Reason: "remote_out_of_order");
        }

        nextState = nextState with { LastRemoteCommandAt = command.EmittedAt };

        if (command.Type == CommandType.RESYNC_REQUEST)
        {
            return new SyncReductionDecision(
                nextState,
                ShouldRequestSnapshot: true,
                Reason: "remote_resync_request");
        }

        if (IsMutatingCommand(command.Type) && command.SourceRole != SourceRole.HOST)
        {
            return new SyncReductionDecision(
                nextState,
                ShouldRequestSnapshot: true,
                ShouldLogWarning: true,
                Reason: "remote_non_host_mutation");
        }

        var operation = MapCommandToOperation(command.Type);
        if (operation == SyncOperation.None)
        {
            return new SyncReductionDecision(nextState, Reason: "remote_ignored_non_media_command");
        }

        if (IsHost(state.Context.Role))
        {
            return new SyncReductionDecision(nextState, Reason: "host_skips_remote_media_apply");
        }

        return new SyncReductionDecision(
            nextState with { PendingLocalSuppression = CreateSuppression(operation, command) },
            ShouldApplyRemoteIntent: true,
            RemoteIntentOperation: operation,
            RemoteIntentSeekPositionMs: TryReadSeekPosition(command),
            Reason: "remote_apply_to_local_media");
    }

    public static SyncReductionDecision ReduceSnapshot(
        SyncReducerState state,
        RoomSnapshot snapshot,
        SyncEngineOptions options)
    {
        if (snapshot.Revision < state.LastSnapshotRevision)
        {
            return new SyncReductionDecision(state, Reason: "snapshot_stale_revision");
        }

        var nextState = state with
        {
            LastSnapshotRevision = snapshot.Revision,
            LastKnownRoomHostId = snapshot.HostId
        };

        var local = state.LastLocalState;
        if (local is null)
        {
            return new SyncReductionDecision(nextState, Reason: "snapshot_no_local_state");
        }

        var playbackMismatch = local.PlaybackStatus != snapshot.Playback.Status;
        var positionMismatch = local.PositionMs is not null &&
                               Math.Abs(local.PositionMs.Value - snapshot.Playback.PositionMs) > options.PositionToleranceMs;
        var titleMismatch = !string.IsNullOrWhiteSpace(local.Title) &&
                            !string.IsNullOrWhiteSpace(snapshot.Playback.Title) &&
                            !string.Equals(local.Title, snapshot.Playback.Title, StringComparison.Ordinal);
        var sourceMismatch = !string.IsNullOrWhiteSpace(local.SourceAppId) &&
                             !string.IsNullOrWhiteSpace(snapshot.Playback.Title) &&
                             string.Equals(local.SourceAppId, snapshot.Playback.Title, StringComparison.Ordinal) == false &&
                             local.Title.Length == 0;

        if (!(playbackMismatch || positionMismatch || titleMismatch || sourceMismatch))
        {
            return new SyncReductionDecision(nextState, Reason: "snapshot_coarse_match");
        }

        if (IsHost(state.Context.Role))
        {
            return new SyncReductionDecision(
                nextState,
                ShouldRequestSnapshot: true,
                ShouldLogWarning: true,
                Reason: "snapshot_host_conflict");
        }

        return new SyncReductionDecision(
            nextState with
            {
                PendingLocalSuppression = new SyncSuppression(
                    snapshot.Playback.Status == PlaybackStatus.PLAYING ? SyncOperation.Play : SyncOperation.Pause,
                    snapshot.Playback.PositionMs)
            },
            ShouldApplySnapshot: true,
            Reason: "snapshot_apply_guest_reconcile");
    }

    public static SyncReducerState RegisterLocalCommandEmission(
        SyncReducerState state,
        string commandId,
        SyncEngineOptions options)
    {
        return state with
        {
            LocalCommandIdsAwaitingEcho = AppendKnownCommandId(
                state.LocalCommandIdsAwaitingEcho ?? [],
                commandId,
                options.MaxKnownCommandIds)
        };
    }

    private static bool IsHost(SourceRole role) => role == SourceRole.HOST;

    private static bool IsMutatingCommand(CommandType type)
        => type is CommandType.PLAY or CommandType.PAUSE or CommandType.SEEK or CommandType.PLAYER_CHANGED or CommandType.READY;

    private static SyncOperation SelectLocalOperation(
        SyncObservedState previous,
        SyncObservedState current,
        SyncEngineOptions options)
    {
        if (previous.PlaybackStatus != current.PlaybackStatus)
        {
            return MapPlaybackOperation(current.PlaybackStatus);
        }

        if (previous.PositionMs is not null &&
            current.PositionMs is not null &&
            Math.Abs(previous.PositionMs.Value - current.PositionMs.Value) >= options.SeekPublishDeltaMs)
        {
            return SyncOperation.Seek;
        }

        if (!string.Equals(previous.SourceAppId, current.SourceAppId, StringComparison.Ordinal) ||
            !string.Equals(previous.Title, current.Title, StringComparison.Ordinal))
        {
            return SyncOperation.PlayerChanged;
        }

        return SyncOperation.None;
    }

    private static SyncOperation MapPlaybackOperation(PlaybackStatus status)
        => status == PlaybackStatus.PLAYING ? SyncOperation.Play : SyncOperation.Pause;

    private static SyncOperation MapCommandToOperation(CommandType type)
        => type switch
        {
            CommandType.PLAY => SyncOperation.Play,
            CommandType.PAUSE => SyncOperation.Pause,
            CommandType.SEEK => SyncOperation.Seek,
            CommandType.PLAYER_CHANGED => SyncOperation.PlayerChanged,
            _ => SyncOperation.None
        };

    private static SyncSuppression CreateSuppression(SyncOperation operation, RoomCommand command)
        => new(operation, operation == SyncOperation.Seek ? TryReadSeekPosition(command) : null);

    private static long? TryReadSeekPosition(RoomCommand command)
    {
        return command.Payload.TryGetProperty("positionMs", out var property) && property.TryGetInt64(out var value)
            ? value
            : null;
    }

    private static bool MatchesSuppression(SyncSuppression suppression, SyncObservedState observedState, SyncEngineOptions options)
    {
        if (suppression.Operation is SyncOperation.Play or SyncOperation.Pause)
        {
            var expected = suppression.Operation == SyncOperation.Play ? PlaybackStatus.PLAYING : PlaybackStatus.PAUSED;
            return observedState.PlaybackStatus == expected;
        }

        if (suppression.Operation == SyncOperation.Seek && suppression.PositionMs is not null && observedState.PositionMs is not null)
        {
            return Math.Abs(suppression.PositionMs.Value - observedState.PositionMs.Value) <= options.PositionToleranceMs;
        }

        return false;
    }

    private static bool ContainsCommandId(IReadOnlyList<string> ids, string commandId)
        => ids.Any(id => string.Equals(id, commandId, StringComparison.Ordinal));

    private static IReadOnlyList<string> RemoveCommandId(IReadOnlyList<string> ids, string commandId)
        => ids.Where(id => !string.Equals(id, commandId, StringComparison.Ordinal)).ToArray();

    private static IReadOnlyList<string> AppendKnownCommandId(IReadOnlyList<string> knownCommandIds, string commandId, int maxKnownCommandIds)
    {
        if (ContainsCommandId(knownCommandIds, commandId))
        {
            return knownCommandIds;
        }

        var next = knownCommandIds.Concat([commandId]).ToArray();
        if (next.Length <= maxKnownCommandIds)
        {
            return next;
        }

        return next.Skip(next.Length - maxKnownCommandIds).ToArray();
    }
}
