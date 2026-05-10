using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayPair.Contracts.Serialization;

public static class ContractJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, SerializerOptions);
}
