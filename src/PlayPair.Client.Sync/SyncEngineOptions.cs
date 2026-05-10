namespace PlayPair.Client.Sync;

public sealed record SyncEngineOptions(
    long PositionToleranceMs = 1_500,
    long SeekPublishDeltaMs = 1_000,
    int MaxKnownCommandIds = 256,
    TimeSpan OutOfOrderTolerance = default)
{
    public TimeSpan EffectiveOutOfOrderTolerance
        => OutOfOrderTolerance == default ? TimeSpan.FromSeconds(2) : OutOfOrderTolerance;
}
