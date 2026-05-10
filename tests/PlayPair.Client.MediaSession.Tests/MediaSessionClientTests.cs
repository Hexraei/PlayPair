using Microsoft.Extensions.Logging.Abstractions;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.MediaSession.Platform;

namespace PlayPair.Client.MediaSession.Tests;

public sealed class MediaSessionClientTests
{
    [Fact]
    public async Task ApplyIntentAsync_ReturnsUnsupportedCapability_WhenIntentCannotBeApplied()
    {
        var session = new FakeSystemMediaSession
        {
            PlaybackInfo = new SystemPlaybackInfo(SystemPlaybackStatus.Paused, canPlay: false, canPause: true, canSeek: false)
        };

        var manager = new FakeSystemMediaSessionManager(session);
        await using var client = new MediaSessionClient(manager, NullLogger<MediaSessionClient>.Instance);
        await client.StartAsync(CancellationToken.None);

        var result = await client.ApplyIntentAsync(new MediaControlIntent(MediaControlIntentType.Play), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("unsupported_capability", result.Code);
    }

    [Fact]
    public async Task StartAsync_PublishesNotDetected_WhenManagerIsUnavailable()
    {
        var manager = new FakeSystemMediaSessionManager(session: null, initializeResult: false);
        await using var client = new MediaSessionClient(manager, NullLogger<MediaSessionClient>.Instance);
        MediaSessionState? published = null;
        client.StateChanged += (_, args) => published = args.State;

        await client.StartAsync(CancellationToken.None);

        Assert.NotNull(published);
        Assert.Equal(MediaSessionCompatibility.NotDetected, published!.Compatibility);
        Assert.False(published.SessionDetected);
    }

    [Fact]
    public async Task SessionEvents_TranslateToStateUpdates()
    {
        var session = new FakeSystemMediaSession
        {
            SourceAppUserModelId = "test.player",
            PlaybackInfo = new SystemPlaybackInfo(SystemPlaybackStatus.Playing, canPlay: true, canPause: true, canSeek: true),
            Timeline = new SystemTimelineProperties(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(3)),
            MediaProperties = new SystemMediaProperties("Song A")
        };

        var manager = new FakeSystemMediaSessionManager(session);
        await using var client = new MediaSessionClient(manager, NullLogger<MediaSessionClient>.Instance);
        var events = new List<MediaSessionStateChangedEventArgs>();
        client.StateChanged += (_, args) => events.Add(args);

        await client.StartAsync(CancellationToken.None);

        session.PlaybackInfo = new SystemPlaybackInfo(SystemPlaybackStatus.Paused, canPlay: true, canPause: true, canSeek: true);
        session.Timeline = new SystemTimelineProperties(TimeSpan.FromSeconds(42), TimeSpan.FromMinutes(3));
        session.MediaProperties = new SystemMediaProperties("Song B");
        session.RaisePlaybackChanged();

        await Task.Delay(50);

        var latest = Assert.Single(events.Where(e => e.State.Title == "Song B"));
        Assert.Equal(MediaPlaybackState.Paused, latest.State.PlaybackState);
        Assert.Equal(TimeSpan.FromSeconds(42), latest.State.Position);
        Assert.Equal(MediaSessionCompatibility.Supported, latest.State.Compatibility);
    }

    [Fact]
    public async Task ManagerSessionSwitch_EmitsSessionSwitchFlag()
    {
        var firstSession = new FakeSystemMediaSession
        {
            SourceAppUserModelId = "player.one",
            PlaybackInfo = new SystemPlaybackInfo(SystemPlaybackStatus.Playing, canPlay: true, canPause: true, canSeek: true)
        };

        var secondSession = new FakeSystemMediaSession
        {
            SourceAppUserModelId = "player.two",
            PlaybackInfo = new SystemPlaybackInfo(SystemPlaybackStatus.Paused, canPlay: true, canPause: true, canSeek: false)
        };

        var manager = new FakeSystemMediaSessionManager(firstSession);
        await using var client = new MediaSessionClient(manager, NullLogger<MediaSessionClient>.Instance);
        var events = new List<MediaSessionStateChangedEventArgs>();
        client.StateChanged += (_, args) => events.Add(args);

        await client.StartAsync(CancellationToken.None);

        manager.SetCurrentSession(secondSession);
        manager.RaiseCurrentSessionChanged();
        await Task.Delay(50);

        var switched = Assert.Single(events.Where(e => e.State.SourceAppId == "player.two"));
        Assert.True(switched.SessionSwitched);
        Assert.Equal(MediaSessionCompatibility.Partial, switched.State.Compatibility);
    }

    private sealed class FakeSystemMediaSessionManager(ISystemMediaSession? session, bool initializeResult = true) : ISystemMediaSessionManager
    {
        private ISystemMediaSession? _session = session;

        public event EventHandler? CurrentSessionChanged;

        public Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(initializeResult);
        }

        public ISystemMediaSession? GetCurrentSession()
        {
            return _session;
        }

        public void SetCurrentSession(ISystemMediaSession? nextSession)
        {
            _session = nextSession;
        }

        public void RaiseCurrentSessionChanged()
        {
            CurrentSessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FakeSystemMediaSession : ISystemMediaSession
    {
        public string SourceAppUserModelId { get; set; } = "test.player";

        public SystemPlaybackInfo PlaybackInfo { get; set; } = new(SystemPlaybackStatus.Paused, true, true, true);

        public SystemTimelineProperties Timeline { get; set; } = new(null, null);

        public SystemMediaProperties MediaProperties { get; set; } = new(string.Empty);

        public event EventHandler? PlaybackInfoChanged;

        public event EventHandler? TimelinePropertiesChanged;

        public event EventHandler? MediaPropertiesChanged;

        public Task<SystemMediaProperties> GetMediaPropertiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(MediaProperties);
        }

        public SystemPlaybackInfo GetPlaybackInfo()
        {
            return PlaybackInfo;
        }

        public SystemTimelineProperties GetTimelineProperties()
        {
            return Timeline;
        }

        public Task<bool> TryPlayAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> TryPauseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> TrySeekAsync(TimeSpan position, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public void RaisePlaybackChanged()
        {
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            TimelinePropertiesChanged?.Invoke(this, EventArgs.Empty);
            MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
        }
    }
}
