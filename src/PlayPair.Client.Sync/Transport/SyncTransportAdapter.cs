using System.Text.Json;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.Sync.Reducer;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Transport;

public static class SyncTransportAdapter
{
    public static SyncObservedState ToObservedState(MediaSessionState state)
    {
        return new SyncObservedState(
            MapPlaybackStatus(state.PlaybackState),
            state.Position is null ? null : (long)state.Position.Value.TotalMilliseconds,
            state.SourceAppId,
            state.Title,
            state.ObservedAtUtc);
    }

    public static RoomCommand BuildRoomCommand(
        SyncSessionContext context,
        SyncOperation operation,
        SyncObservedState state)
    {
        var payload = operation switch
        {
            SyncOperation.Seek => JsonSerializer.SerializeToElement(new
            {
                positionMs = state.PositionMs ?? 0L
            }),
            SyncOperation.PlayerChanged => JsonSerializer.SerializeToElement(new
            {
                sourceAppId = state.SourceAppId,
                title = state.Title
            }),
            _ => JsonSerializer.SerializeToElement(new { })
        };

        var type = operation switch
        {
            SyncOperation.Play => CommandType.PLAY,
            SyncOperation.Pause => CommandType.PAUSE,
            SyncOperation.Seek => CommandType.SEEK,
            SyncOperation.PlayerChanged => CommandType.PLAYER_CHANGED,
            _ => throw new InvalidOperationException($"Unsupported operation {operation}")
        };

        return new RoomCommand
        {
            RoomId = context.RoomId,
            SourceClientId = context.ClientId,
            SourceRole = context.Role,
            Type = type,
            Payload = payload,
            EmittedAt = DateTimeOffset.UtcNow
        };
    }

    public static RoomCommand BuildResyncRequest(SyncSessionContext context)
    {
        return new RoomCommand
        {
            RoomId = context.RoomId,
            SourceClientId = context.ClientId,
            SourceRole = context.Role,
            Type = CommandType.RESYNC_REQUEST,
            Payload = JsonSerializer.SerializeToElement(new { reason = "client_reconcile" }),
            EmittedAt = DateTimeOffset.UtcNow
        };
    }

    public static MediaControlIntent? ToMediaIntent(RoomCommand command, SyncOperation operation)
    {
        return operation switch
        {
            SyncOperation.Play => new MediaControlIntent(MediaControlIntentType.Play),
            SyncOperation.Pause => new MediaControlIntent(MediaControlIntentType.Pause),
            SyncOperation.Seek => TryBuildSeekIntent(command),
            _ => null
        };
    }

    public static IReadOnlyList<MediaControlIntent> ToSnapshotIntents(
        RoomSnapshot snapshot,
        SyncObservedState? localState,
        SyncEngineOptions options)
    {
        var intents = new List<MediaControlIntent>();
        var targetState = snapshot.Playback.Status == PlaybackStatus.PLAYING
            ? MediaControlIntentType.Play
            : MediaControlIntentType.Pause;

        if (localState is null || localState.PlaybackStatus != snapshot.Playback.Status)
        {
            intents.Add(new MediaControlIntent(targetState));
        }

        if (localState?.PositionMs is null ||
            Math.Abs(localState.PositionMs.Value - snapshot.Playback.PositionMs) > options.PositionToleranceMs)
        {
            intents.Add(new MediaControlIntent(MediaControlIntentType.Seek, TimeSpan.FromMilliseconds(snapshot.Playback.PositionMs)));
        }

        return intents;
    }

    private static MediaControlIntent? TryBuildSeekIntent(RoomCommand command)
    {
        if (!command.Payload.TryGetProperty("positionMs", out var property) || !property.TryGetInt64(out var positionMs))
        {
            return null;
        }

        return new MediaControlIntent(MediaControlIntentType.Seek, TimeSpan.FromMilliseconds(positionMs));
    }

    private static PlaybackStatus MapPlaybackStatus(MediaPlaybackState playbackState)
        => playbackState == MediaPlaybackState.Playing ? PlaybackStatus.PLAYING : PlaybackStatus.PAUSED;
}
