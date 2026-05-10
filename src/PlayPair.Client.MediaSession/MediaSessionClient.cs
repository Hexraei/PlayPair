using Microsoft.Extensions.Logging;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.MediaSession.Compatibility;
using PlayPair.Client.MediaSession.Platform;

namespace PlayPair.Client.MediaSession;

public sealed class MediaSessionClient : IMediaSessionClient
{
    private readonly ISystemMediaSessionManager _sessionManager;
    private readonly ILogger<MediaSessionClient> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private ISystemMediaSession? _activeSession;
    private string? _activeSessionId;
    private bool _isStarted;

    public MediaSessionClient(ILogger<MediaSessionClient> logger)
        : this(new WindowsSystemMediaSessionManager(), logger)
    {
    }

    internal MediaSessionClient(ISystemMediaSessionManager sessionManager, ILogger<MediaSessionClient> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public event EventHandler<MediaSessionStateChangedEventArgs>? StateChanged;

    public MediaSessionState CurrentState { get; private set; } = MediaSessionState.NotDetected;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;

        var initialized = await _sessionManager.InitializeAsync(cancellationToken);
        if (!initialized)
        {
            _logger.LogWarning("MediaSessionInitializeFailed {Compatibility}", MediaSessionCompatibility.NotDetected);
            PublishState(MediaSessionState.NotDetected, false);
            return;
        }

        _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
        _logger.LogInformation("MediaSessionInitialized");
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            await RefreshLockedAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<MediaControlResult> ApplyIntentAsync(MediaControlIntent intent, CancellationToken cancellationToken)
    {
        if (_activeSession is null || !CurrentState.SessionDetected)
        {
            _logger.LogWarning("MediaIntentRejectedNoSession {IntentType}", intent.Type);
            return MediaControlResult.Failure("no_session", "No active media session was detected.");
        }

        var playbackInfo = _activeSession.GetPlaybackInfo();
        var capabilities = new MediaSessionCapabilities(playbackInfo.CanPlay, playbackInfo.CanPause, playbackInfo.CanSeek);

        var result = intent.Type switch
        {
            MediaControlIntentType.Play => await TryApplyAsync(intent.Type, capabilities.CanPlay, () => _activeSession.TryPlayAsync(cancellationToken)),
            MediaControlIntentType.Pause => await TryApplyAsync(intent.Type, capabilities.CanPause, () => _activeSession.TryPauseAsync(cancellationToken)),
            MediaControlIntentType.Seek => await TryApplySeekAsync(intent, capabilities.CanSeek, cancellationToken),
            _ => MediaControlResult.Failure("unsupported_intent", $"Unsupported intent {intent.Type}.")
        };

        if (result.Succeeded)
        {
            await RefreshAsync(cancellationToken);
        }

        return result;
    }

    public ValueTask DisposeAsync()
    {
        if (_isStarted)
        {
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
            UnsubscribeFromActiveSession();
            _isStarted = false;
        }

        _refreshLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task RefreshLockedAsync(CancellationToken cancellationToken)
    {
        var currentSession = _sessionManager.GetCurrentSession();
        var nextSessionId = currentSession?.SourceAppUserModelId;
        var sessionSwitched = !string.Equals(_activeSessionId, nextSessionId, StringComparison.Ordinal);

        if (sessionSwitched)
        {
            _logger.LogInformation("MediaSessionSwitchDetected {PreviousSessionId} {CurrentSessionId}", _activeSessionId ?? "none", nextSessionId ?? "none");
            UnsubscribeFromActiveSession();
            _activeSession = currentSession;
            _activeSessionId = nextSessionId;
            SubscribeToActiveSession();
        }
        else if (currentSession is not null && !ReferenceEquals(currentSession, _activeSession))
        {
            currentSession.Dispose();
        }

        if (_activeSession is null)
        {
            PublishState(MediaSessionState.NotDetected with { ObservedAtUtc = DateTimeOffset.UtcNow }, sessionSwitched);
            return;
        }

        var nextState = await BuildStateAsync(_activeSession, cancellationToken);
        PublishState(nextState, sessionSwitched);
    }

    private async Task<MediaSessionState> BuildStateAsync(ISystemMediaSession session, CancellationToken cancellationToken)
    {
        try
        {
            var playbackInfo = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();
            var mediaProperties = await session.GetMediaPropertiesAsync(cancellationToken);

            var capabilities = new MediaSessionCapabilities(
                playbackInfo.CanPlay,
                playbackInfo.CanPause,
                playbackInfo.CanSeek);

            var compatibility = MediaSessionCompatibilityMapper.Map(true, capabilities);

            return new MediaSessionState(
                compatibility,
                MapPlaybackState(playbackInfo.Status),
                capabilities,
                timeline.Position,
                timeline.Duration,
                session.SourceAppUserModelId,
                mediaProperties.Title,
                true,
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MediaSessionReadFailed {SessionId}", session.SourceAppUserModelId);
            return MediaSessionState.NotDetected with { ObservedAtUtc = DateTimeOffset.UtcNow };
        }
    }

    private async Task<MediaControlResult> TryApplySeekAsync(MediaControlIntent intent, bool canSeek, CancellationToken cancellationToken)
    {
        if (intent.SeekPosition is null)
        {
            _logger.LogWarning("MediaIntentRejectedInvalidSeekPayload");
            return MediaControlResult.Failure("invalid_seek", "Seek requires a target position.");
        }

        return await TryApplyAsync(
            intent.Type,
            canSeek,
            () => _activeSession!.TrySeekAsync(intent.SeekPosition.Value, cancellationToken));
    }

    private async Task<MediaControlResult> TryApplyAsync(MediaControlIntentType intentType, bool capabilityEnabled, Func<Task<bool>> applyCommand)
    {
        if (!capabilityEnabled)
        {
            _logger.LogWarning("MediaIntentRejectedUnsupportedCapability {IntentType}", intentType);
            return MediaControlResult.Failure("unsupported_capability", $"Intent {intentType} is not supported by active session.");
        }

        try
        {
            var succeeded = await applyCommand();
            if (!succeeded)
            {
                _logger.LogWarning("MediaIntentRejectedBySession {IntentType}", intentType);
                return MediaControlResult.Failure("session_rejected", $"Session rejected {intentType} command.");
            }

            _logger.LogInformation("MediaIntentApplied {IntentType}", intentType);
            return MediaControlResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MediaIntentApplyFailed {IntentType}", intentType);
            return MediaControlResult.Failure("command_error", $"Failed to apply {intentType} command.");
        }
    }

    private void PublishState(MediaSessionState state, bool sessionSwitched)
    {
        CurrentState = state;
        _logger.LogInformation(
            "MediaSessionStateUpdated {Compatibility} {PlaybackState} {SessionId} {SessionSwitched}",
            state.Compatibility,
            state.PlaybackState,
            string.IsNullOrWhiteSpace(state.SourceAppId) ? "none" : state.SourceAppId,
            sessionSwitched);

        StateChanged?.Invoke(this, new MediaSessionStateChangedEventArgs(state, sessionSwitched));
    }

    private void OnCurrentSessionChanged(object? sender, EventArgs args)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    private void OnPlaybackInfoChanged(object? sender, EventArgs args)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    private void OnTimelinePropertiesChanged(object? sender, EventArgs args)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    private void OnMediaPropertiesChanged(object? sender, EventArgs args)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    private void SubscribeToActiveSession()
    {
        if (_activeSession is null)
        {
            return;
        }

        _activeSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
        _activeSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        _activeSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
    }

    private void UnsubscribeFromActiveSession()
    {
        if (_activeSession is null)
        {
            return;
        }

        _activeSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _activeSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _activeSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _activeSession.Dispose();
        _activeSession = null;
        _activeSessionId = null;
    }

    private static MediaPlaybackState MapPlaybackState(SystemPlaybackStatus status)
    {
        return status switch
        {
            SystemPlaybackStatus.Playing => MediaPlaybackState.Playing,
            SystemPlaybackStatus.Paused => MediaPlaybackState.Paused,
            _ => MediaPlaybackState.Unknown
        };
    }
}
