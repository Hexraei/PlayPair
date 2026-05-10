using System.Text.Json.Serialization;

namespace PlayPair.Contracts.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SourceRole
{
    HOST,
    GUEST,
    SYSTEM
}
