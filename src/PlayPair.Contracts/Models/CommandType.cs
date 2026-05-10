using System.Text.Json.Serialization;

namespace PlayPair.Contracts.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandType
{
    PLAY,
    PAUSE,
    SEEK,
    READY,
    RESYNC_REQUEST,
    PLAYER_CHANGED
}
