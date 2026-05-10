using System.Text.Json;
using Microsoft.Extensions.Logging;
using PlayPair.Contracts.Models;
using PlayPair.Server.Rooms;

namespace PlayPair.Server.Tests;

public class InMemoryRoomManagerTests
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });

    [Fact]
    public void CreateRoom_AssignsHostAndGeneratesRoomCode()
    {
        var manager = CreateManager();

        var result = manager.CreateRoom("conn-host", "Host");

        Assert.Matches("^[A-Z0-9]{6}$", result.RoomCode);
        Assert.Equal(SourceRole.HOST, result.AssignedRole);
        Assert.NotEqual("conn-host", result.ClientId);
        Assert.Equal(result.RoomCode, result.Snapshot.RoomId);
        Assert.Equal(result.ClientId, result.Snapshot.HostId);
        Assert.Single(result.Snapshot.Participants);
    }

    [Fact]
    public void JoinRoom_AssignsGuest_AndEnforcesTwoUserLimit()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");

        var guest = manager.JoinRoom("conn-guest", host.RoomCode, "Guest");

        Assert.Equal(SourceRole.GUEST, guest.AssignedRole);
        Assert.Equal(2, guest.Snapshot.Participants.Count);
        var error = Assert.Throws<RoomOperationException>(() => manager.JoinRoom("conn-extra", host.RoomCode, "Extra"));
        Assert.Contains("full", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LeaveRoom_HostLeaves_PromotesGuestToHost()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        var guest = manager.JoinRoom("conn-guest", host.RoomCode, "Guest");

        var leave = manager.LeaveRoom("conn-host");

        Assert.False(leave.RoomClosed);
        Assert.NotNull(leave.Snapshot);
        Assert.Equal(guest.ClientId, leave.Snapshot!.HostId);
        Assert.Single(leave.Snapshot.Participants);
        Assert.Equal(SourceRole.HOST, leave.Snapshot.Participants[0].Role);
    }

    [Fact]
    public void ProcessCommand_GuestMutatingCommand_IsRejected()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        var guest = manager.JoinRoom("conn-guest", host.RoomCode, "Guest");
        var command = BuildCommand(host.RoomCode, guest.ClientId, SourceRole.GUEST, CommandType.PAUSE, new { });

        var error = Assert.Throws<RoomOperationException>(() => manager.ProcessCommand("conn-guest", command));

        Assert.Contains("Only host", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessCommand_HostPlay_UpdatesSnapshot()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        var command = BuildCommand(host.RoomCode, host.ClientId, SourceRole.HOST, CommandType.PLAY, new { });

        var relay = manager.ProcessCommand("conn-host", command);

        Assert.Equal(CommandProcessStatus.Accepted, relay.Status);
        Assert.NotNull(relay.Snapshot);
        Assert.Equal(PlaybackStatus.PLAYING, relay.Snapshot!.Playback.Status);
        Assert.Equal(command.CommandId, relay.Snapshot.LastCommandId);
    }

    [Fact]
    public void ProcessCommand_DuplicateCommand_IsIdempotent()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        var command = BuildCommand(host.RoomCode, host.ClientId, SourceRole.HOST, CommandType.PLAY, new { });

        var first = manager.ProcessCommand("conn-host", command);
        var duplicate = manager.ProcessCommand("conn-host", command);

        Assert.Equal(CommandProcessStatus.Accepted, first.Status);
        Assert.Equal(CommandProcessStatus.Duplicate, duplicate.Status);
        Assert.Equal(first.Snapshot?.Revision, duplicate.Snapshot?.Revision);
    }

    [Fact]
    public void ProcessCommand_StaleMutatingCommand_IsRejected()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        var newer = BuildCommand(
            host.RoomCode,
            host.ClientId,
            SourceRole.HOST,
            CommandType.PLAY,
            new { },
            emittedAt: DateTimeOffset.UtcNow);
        _ = manager.ProcessCommand("conn-host", newer);
        var older = BuildCommand(
            host.RoomCode,
            host.ClientId,
            SourceRole.HOST,
            CommandType.PAUSE,
            new { },
            emittedAt: newer.EmittedAt.AddSeconds(-2));

        var error = Assert.Throws<RoomOperationException>(() => manager.ProcessCommand("conn-host", older));

        Assert.Contains("Stale", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("stale_command", error.Code);
    }

    [Fact]
    public void Reconnect_AndRecoverState_ReturnsCurrentSnapshot()
    {
        var manager = CreateManager();
        var host = manager.CreateRoom("conn-host", "Host");
        _ = manager.MarkDisconnected("conn-host");

        var reconnect = manager.Reconnect("conn-host-new", host.RoomCode, host.ClientId);
        var recovered = manager.RecoverState("conn-host-new", host.RoomCode, host.ClientId);

        Assert.Equal(host.ClientId, reconnect.ClientId);
        Assert.Equal(host.RoomCode, recovered.RoomId);
        Assert.Equal(host.ClientId, recovered.HostId);
    }

    [Fact]
    public void JoinRoom_InvalidCode_IsRejected()
    {
        var manager = CreateManager();

        var error = Assert.Throws<RoomOperationException>(() => manager.JoinRoom("conn-guest", "abc", "Guest"));

        Assert.Contains("roomCode", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static InMemoryRoomManager CreateManager()
        => new(LoggerFactory.CreateLogger<InMemoryRoomManager>());

    private static RoomCommand BuildCommand(
        string roomCode,
        string sourceClientId,
        SourceRole sourceRole,
        CommandType type,
        object payload,
        DateTimeOffset? emittedAt = null)
        => new()
        {
            CommandId = Guid.NewGuid().ToString("N"),
            RoomId = roomCode,
            SourceClientId = sourceClientId,
            SourceRole = sourceRole,
            Type = type,
            ProtocolVersion = 1,
            EmittedAt = emittedAt ?? DateTimeOffset.UtcNow,
            Payload = JsonSerializer.SerializeToElement(payload)
        };
}
