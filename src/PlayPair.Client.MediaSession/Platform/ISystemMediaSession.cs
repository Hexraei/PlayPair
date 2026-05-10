namespace PlayPair.Client.MediaSession.Platform;

internal interface ISystemMediaSession : IDisposable
{
    string SourceAppUserModelId { get; }

    event EventHandler? PlaybackInfoChanged;

    event EventHandler? TimelinePropertiesChanged;

    event EventHandler? MediaPropertiesChanged;

    Task<SystemMediaProperties> GetMediaPropertiesAsync(CancellationToken cancellationToken);

    SystemPlaybackInfo GetPlaybackInfo();

    SystemTimelineProperties GetTimelineProperties();

    Task<bool> TryPlayAsync(CancellationToken cancellationToken);

    Task<bool> TryPauseAsync(CancellationToken cancellationToken);

    Task<bool> TrySeekAsync(TimeSpan position, CancellationToken cancellationToken);
}
