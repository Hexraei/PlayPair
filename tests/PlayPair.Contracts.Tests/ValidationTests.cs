using System.Text.Json;
using PlayPair.Contracts.Models;
using PlayPair.Contracts.Validation;

namespace PlayPair.Contracts.Tests;

public class ValidationTests
{
    [Fact]
    public void ValidateCommand_ValidSeekCommand_Passes()
    {
        var command = CreateValidCommand(
            CommandType.SEEK,
            JsonSerializer.SerializeToElement(new { positionMs = 2500L }));

        var result = ContractValidator.ValidateCommand(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCommand_MissingIds_Fails()
    {
        var command = CreateValidCommand(
            CommandType.PLAY,
            JsonSerializer.SerializeToElement(new { }))
            with
            {
                CommandId = "",
                RoomId = "",
                SourceClientId = ""
            };

        var result = ContractValidator.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("commandId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("roomId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("sourceClientId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateCommand_InvalidType_Fails()
    {
        var command = CreateValidCommand(
            (CommandType)999,
            JsonSerializer.SerializeToElement(new { }));

        var result = ContractValidator.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("type is invalid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateCommand_NegativeSeekPosition_Fails()
    {
        var command = CreateValidCommand(
            CommandType.SEEK,
            JsonSerializer.SerializeToElement(new { positionMs = -1L }));

        var result = ContractValidator.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot be negative", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateCommand_ReadyWithoutBooleanPayload_Fails()
    {
        var command = CreateValidCommand(
            CommandType.READY,
            JsonSerializer.SerializeToElement(new { isReady = "yes" }));

        var result = ContractValidator.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("isReady", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateCommand_PlayerChangedWithoutPlayerId_Fails()
    {
        var command = CreateValidCommand(
            CommandType.PLAYER_CHANGED,
            JsonSerializer.SerializeToElement(new { playerClientId = "" }));

        var result = ContractValidator.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("playerClientId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateSnapshot_NegativePosition_Fails()
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
                Status = PlaybackStatus.PAUSED,
                PositionMs = -12,
                Title = "Sample"
            },
            Revision = 1,
            LastCommandId = "cmd_123"
        };

        var result = ContractValidator.ValidateSnapshot(snapshot);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("playback.positionMs", StringComparison.OrdinalIgnoreCase));
    }

    private static RoomCommand CreateValidCommand(CommandType type, JsonElement payload)
        => new()
        {
            CommandId = "cmd_123",
            RoomId = "room-1",
            Type = type,
            SourceRole = SourceRole.HOST,
            SourceClientId = "client-1",
            EmittedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            ProtocolVersion = 1,
            Payload = payload
        };
}
