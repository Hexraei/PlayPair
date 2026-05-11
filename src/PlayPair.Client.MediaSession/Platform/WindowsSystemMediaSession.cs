using Windows.Media.Control;

namespace PlayPair.Client.MediaSession.Platform;

internal sealed class WindowsSystemMediaSession : ISystemMediaSession
{
    private readonly GlobalSystemMediaTransportControlsSession _innerSession;

    public WindowsSystemMediaSession(GlobalSystemMediaTransportControlsSession innerSession)
    {
        _innerSession = innerSession;
        _innerSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
        _innerSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        _innerSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
    }

    public string SourceAppUserModelId => _innerSession.SourceAppUserModelId;

    public event EventHandler? PlaybackInfoChanged;

    public event EventHandler? TimelinePropertiesChanged;

    public event EventHandler? MediaPropertiesChanged;

    public async Task<SystemMediaProperties> GetMediaPropertiesAsync(CancellationToken cancellationToken)
    {
        var properties = await _innerSession.TryGetMediaPropertiesAsync().AsTask(cancellationToken);
        return new SystemMediaProperties(properties.Title ?? string.Empty);
    }

    public SystemPlaybackInfo GetPlaybackInfo()
    {
        var info = _innerSession.GetPlaybackInfo();
        return new SystemPlaybackInfo(
            MapStatus(info.PlaybackStatus),
            info.Controls.IsPlayEnabled,
            info.Controls.IsPauseEnabled,
            info.Controls.IsPlaybackPositionEnabled);
    }

    public SystemTimelineProperties GetTimelineProperties()
    {
        var timeline = _innerSession.GetTimelineProperties();
        return new SystemTimelineProperties(timeline.Position, timeline.EndTime);
    }

    public Task<bool> TryPlayAsync(CancellationToken cancellationToken)
    {
        return _innerSession.TryPlayAsync().AsTask(cancellationToken);
    }

    public Task<bool> TryPauseAsync(CancellationToken cancellationToken)
    {
        return _innerSession.TryPauseAsync().AsTask(cancellationToken);
    }

    public Task<bool> TrySeekAsync(TimeSpan position, CancellationToken cancellationToken)
    {
        if (position < TimeSpan.Zero)
        {
            return Task.FromResult(false);
        }

        var ticks = position.Ticks;
        return _innerSession.TryChangePlaybackPositionAsync(ticks).AsTask(cancellationToken);
    }

    public void Dispose()
    {
        _innerSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _innerSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _innerSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
    }

    private static SystemPlaybackStatus MapStatus(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => SystemPlaybackStatus.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => SystemPlaybackStatus.Paused,
            _ => SystemPlaybackStatus.Unknown
        };
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        TimelinePropertiesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
    }
}
