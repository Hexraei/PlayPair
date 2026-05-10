namespace PlayPair.Client.MediaSession.Abstractions;

public enum MediaSessionCompatibility
{
    Supported,
    Partial,
    NotDetected
}

public enum MediaPlaybackState
{
    Playing,
    Paused,
    Unknown
}

public sealed record MediaSessionCapabilities(
    bool CanPlay,
    bool CanPause,
    bool CanSeek)
{
    public static MediaSessionCapabilities None { get; } = new(false, false, false);
}

public sealed record MediaSessionState(
    MediaSessionCompatibility Compatibility,
    MediaPlaybackState PlaybackState,
    MediaSessionCapabilities Capabilities,
    TimeSpan? Position,
    TimeSpan? Duration,
    string SourceAppId,
    string Title,
    bool SessionDetected,
    DateTimeOffset ObservedAtUtc)
{
    public static MediaSessionState NotDetected { get; } = new(
        MediaSessionCompatibility.NotDetected,
        MediaPlaybackState.Unknown,
        MediaSessionCapabilities.None,
        null,
        null,
        string.Empty,
        string.Empty,
        false,
        DateTimeOffset.UtcNow);
}

public sealed class MediaSessionStateChangedEventArgs(MediaSessionState state, bool sessionSwitched) : EventArgs
{
    public MediaSessionState State { get; } = state;

    public bool SessionSwitched { get; } = sessionSwitched;
}
