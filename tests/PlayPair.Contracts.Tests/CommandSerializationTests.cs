using System.Text.Json;
using PlayPair.Contracts.Models;
using PlayPair.Contracts.Serialization;

namespace PlayPair.Contracts.Tests;

public class CommandSerializationTests
{
    [Fact]
    public void RoomCommand_Serializes_WithExpectedShape()
    {
        var command = new RoomCommand
        {
            CommandId = "cmd_123",
            RoomId = "room-1",
            Type = CommandType.SEEK,
            SourceRole = SourceRole.HOST,
            SourceClientId = "client-1",
            EmittedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            ProtocolVersion = 1,
            Payload = JsonSerializer.SerializeToElement(new { positionMs = 1250L })
        };

        var json = ContractJson.Serialize(command);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("cmd_123", root.GetProperty("commandId").GetString());
        Assert.Equal("room-1", root.GetProperty("roomId").GetString());
        Assert.Equal("SEEK", root.GetProperty("type").GetString());
        Assert.Equal("HOST", root.GetProperty("sourceRole").GetString());
        Assert.Equal("client-1", root.GetProperty("sourceClientId").GetString());
        Assert.Equal(1, root.GetProperty("protocolVersion").GetInt32());
        Assert.Equal(1250L, root.GetProperty("payload").GetProperty("positionMs").GetInt64());
    }

    [Fact]
    public void RoomSnapshot_Serializes_WithExpectedShape()
    {
        var snapshot = new RoomSnapshot
        {
            RoomId = "room-1",
            HostId = "host-1",
            Participants =
            [
                new ParticipantSnapshot
                {
                    ClientId = "host-1",
                    DisplayName = "Host",
                    Role = SourceRole.HOST,
                    IsReady = true
                }
            ],
            Playback = new PlaybackSnapshot
            {
                Status = PlaybackStatus.PLAYING,
                PositionMs = 900,
                Title = "Sample Video"
            },
            Revision = 2,
            LastCommandId = "cmd_123"
        };

        var json = ContractJson.Serialize(snapshot);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("room-1", root.GetProperty("roomId").GetString());
        Assert.Equal("host-1", root.GetProperty("hostId").GetString());
        Assert.Equal(2, root.GetProperty("revision").GetInt32());
        Assert.Equal("cmd_123", root.GetProperty("lastCommandId").GetString());

        var playback = root.GetProperty("playback");
        Assert.Equal("PLAYING", playback.GetProperty("status").GetString());
        Assert.Equal(900, playback.GetProperty("positionMs").GetInt32());
        Assert.Equal("Sample Video", playback.GetProperty("title").GetString());
    }
}
