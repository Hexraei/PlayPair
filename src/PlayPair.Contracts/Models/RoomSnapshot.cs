using System.Text.Json.Serialization;

namespace PlayPair.Contracts.Models;

public sealed record ParticipantSnapshot
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public SourceRole Role { get; init; }

    [JsonPropertyName("isReady")]
    public bool IsReady { get; init; }
}

public sealed record PlaybackSnapshot
{
    [JsonPropertyName("status")]
    public PlaybackStatus Status { get; init; }

    [JsonPropertyName("positionMs")]
    public long PositionMs { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}

public sealed record RoomSnapshot
{
    [JsonPropertyName("roomId")]
    public string RoomId { get; init; } = string.Empty;

    [JsonPropertyName("hostId")]
    public string HostId { get; init; } = string.Empty;

    [JsonPropertyName("participants")]
    public IReadOnlyList<ParticipantSnapshot> Participants { get; init; } = Array.Empty<ParticipantSnapshot>();

    [JsonPropertyName("playback")]
    public PlaybackSnapshot Playback { get; init; } = new();

    [JsonPropertyName("revision")]
    public long Revision { get; init; }

    [JsonPropertyName("lastCommandId")]
    public string LastCommandId { get; init; } = string.Empty;
}
