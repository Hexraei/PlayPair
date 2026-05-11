using Microsoft.Extensions.Logging.Abstractions;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.Sync.Abstractions;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync.Tests;

public sealed class ClientSyncEngineTests
{
    [Fact]
    public async Task LocalHostStateChange_SendsRemoteCommand()
    {
        var media = new FakeMediaSessionClient();
        var transport = new FakeRoomSyncTransport();
        await using var engine = new ClientSyncEngine(media, transport, NullLogger<ClientSyncEngine>.Instance);

        await engine.StartAsync(new SyncSessionContext("ROOM01", "host-1", SourceRole.HOST), CancellationToken.None);
        media.Publish(CreateMediaState(MediaPlaybackState.Playing, 2_000));
        await Task.Delay(100);

        var command = Assert.Single(transport.SentCommands, c => c.Type == CommandType.PLAY);
        Assert.Equal("host-1", command.SourceClientId);
    }

    [Fact]
    public async Task RemoteCommand_AppliesIntent_WithoutLoopingBackToTransport()
    {
        var media = new FakeMediaSessionClient();
        var transport = new FakeRoomSyncTransport();
        await using var engine = new ClientSyncEngine(media, transport, NullLogger<ClientSyncEngine>.Instance);

        await engine.StartAsync(new SyncSessionContext("ROOM01", "guest-1", SourceRole.GUEST), CancellationToken.None);
        transport.RaiseCommand(new RoomCommand
        {
            CommandId = "cmd-remote-1",
            RoomId = "ROOM01",
            SourceClientId = "host-1",
            SourceRole = SourceRole.HOST,
            Type = CommandType.PLAY
        });
        await Task.Delay(100);
        media.Publish(CreateMediaState(MediaPlaybackState.Playing, 1_000));
        await Task.Delay(100);

        Assert.Contains(media.AppliedIntents, i => i.Type == MediaControlIntentType.Play);
        Assert.DoesNotContain(transport.SentCommands, c => c.Type == CommandType.PLAY);
    }

    [Fact]
    public async Task DuplicateRemoteCommand_IsAppliedOnlyOnce()
    {
        var media = new FakeMediaSessionClient();
        var transport = new FakeRoomSyncTransport();
        await using var engine = new ClientSyncEngine(media, transport, NullLogger<ClientSyncEngine>.Instance);

        await engine.StartAsync(new SyncSessionContext("ROOM01", "guest-1", SourceRole.GUEST), CancellationToken.None);
        var command = new RoomCommand
        {
            CommandId = "cmd-dup",
            RoomId = "ROOM01",
            SourceClientId = "host-1",
            SourceRole = SourceRole.HOST,
            Type = CommandType.PAUSE
        };

        transport.RaiseCommand(command);
        transport.RaiseCommand(command);
        await Task.Delay(120);

        Assert.Equal(1, media.AppliedIntents.Count(i => i.Type == MediaControlIntentType.Pause));
    }

    [Fact]
    public async Task ResyncRequest_RequestsSnapshot_AndAppliesSnapshot()
    {
        var media = new FakeMediaSessionClient();
        var transport = new FakeRoomSyncTransport
        {
            SnapshotToReturn = new RoomSnapshot
            {
                RoomId = "ROOM01",
                HostId = "host-1",
                Revision = 2,
                Playback = new PlaybackSnapshot
                {
                    Status = PlaybackStatus.PAUSED,
                    PositionMs = 12_000
                }
            }
        };

        await using var engine = new ClientSyncEngine(media, transport, NullLogger<ClientSyncEngine>.Instance);
        await engine.StartAsync(new SyncSessionContext("ROOM01", "guest-1", SourceRole.GUEST), CancellationToken.None);
        media.Publish(CreateMediaState(MediaPlaybackState.Playing, 200));

        transport.RaiseCommand(new RoomCommand
        {
            CommandId = "cmd-resync",
            RoomId = "ROOM01",
            SourceClientId = "host-1",
            SourceRole = SourceRole.HOST,
            Type = CommandType.RESYNC_REQUEST
        });
        await Task.Delay(150);

        Assert.Equal(1, transport.SnapshotRequests);
        Assert.Contains(media.AppliedIntents, i => i.Type == MediaControlIntentType.Pause);
        Assert.Contains(media.AppliedIntents, i => i.Type == MediaControlIntentType.Seek);
    }

    private static MediaSessionState CreateMediaState(MediaPlaybackState playbackState, long positionMs)
        => new(
            MediaSessionCompatibility.Supported,
            playbackState,
            new MediaSessionCapabilities(true, true, true),
            TimeSpan.FromMilliseconds(positionMs),
            TimeSpan.FromMinutes(3),
            "spotify",
            "Track",
            true,
            DateTimeOffset.UtcNow);

    private sealed class FakeMediaSessionClient : IMediaSessionClient
    {
        public event EventHandler<MediaSessionStateChangedEventArgs>? StateChanged;

        public MediaSessionState CurrentState { get; private set; } = MediaSessionState.NotDetected;

        public List<MediaControlIntent> AppliedIntents { get; } = [];

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<MediaControlResult> ApplyIntentAsync(MediaControlIntent intent, CancellationToken cancellationToken)
        {
            AppliedIntents.Add(intent);
            return Task.FromResult(MediaControlResult.Success());
        }

        public void Publish(MediaSessionState state)
        {
            CurrentState = state;
            StateChanged?.Invoke(this, new MediaSessionStateChangedEventArgs(state, false));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeRoomSyncTransport : IRoomSyncTransport
    {
        public event EventHandler<RoomCommandReceivedEventArgs>? CommandReceived;

        public event EventHandler<RoomSnapshotReceivedEventArgs>? SnapshotReceived;

        public List<RoomCommand> SentCommands { get; } = [];

        public int SnapshotRequests { get; private set; }

        public RoomSnapshot SnapshotToReturn { get; set; } = new()
        {
            RoomId = "ROOM01",
            HostId = "host-1",
            Playback = new PlaybackSnapshot { Status = PlaybackStatus.PAUSED, PositionMs = 0 },
            Revision = 1
        };

        public Task SendCommandAsync(RoomCommand command, CancellationToken cancellationToken)
        {
            SentCommands.Add(command);
            return Task.CompletedTask;
        }

        public Task<RoomSnapshot> RequestSnapshotAsync(string roomId, CancellationToken cancellationToken)
        {
            SnapshotRequests += 1;
            return Task.FromResult(SnapshotToReturn);
        }

        public void RaiseCommand(RoomCommand command)
        {
            CommandReceived?.Invoke(this, new RoomCommandReceivedEventArgs(command));
        }

        public void RaiseSnapshot(RoomSnapshot snapshot)
        {
            SnapshotReceived?.Invoke(this, new RoomSnapshotReceivedEventArgs(snapshot));
        }
    }
}
