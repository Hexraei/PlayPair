namespace PlayPair.Contracts.Protocol;

public static class ProtocolVersions
{
    public const int V1 = 1;

    public static bool IsSupported(int version) => version == V1;
}
