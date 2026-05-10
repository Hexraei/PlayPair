using System.Text.Json;
using System.Text.Json.Serialization;
using PlayPair.Contracts.Protocol;

namespace PlayPair.Contracts.Models;

public sealed record RoomCommand
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; init; } = CommandIdGenerator.New();

    [JsonPropertyName("roomId")]
    public string RoomId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public CommandType Type { get; init; }

    [JsonPropertyName("sourceRole")]
    public SourceRole SourceRole { get; init; }

    [JsonPropertyName("sourceClientId")]
    public string SourceClientId { get; init; } = string.Empty;

    [JsonPropertyName("emittedAt")]
    public DateTimeOffset EmittedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; init; } = ProtocolVersions.V1;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; } = JsonSerializer.SerializeToElement(new { });
}
