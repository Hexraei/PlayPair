namespace PlayPair.Contracts.Models;

public static class CommandIdGenerator
{
    public static string New() => $"cmd_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
}
