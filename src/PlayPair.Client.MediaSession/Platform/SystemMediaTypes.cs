namespace PlayPair.Client.MediaSession.Platform;

internal enum SystemPlaybackStatus
{
    Unknown,
    Playing,
    Paused
}

internal sealed record SystemPlaybackInfo(
    SystemPlaybackStatus Status,
    bool CanPlay,
    bool CanPause,
    bool CanSeek);

internal sealed record SystemMediaProperties(string Title);

internal sealed record SystemTimelineProperties(TimeSpan? Position, TimeSpan? Duration);
