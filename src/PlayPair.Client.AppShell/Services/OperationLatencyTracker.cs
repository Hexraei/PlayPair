using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayPair.Client.AppShell.Errors;

namespace PlayPair.Client.AppShell.Services;

/// <summary>
/// Tracks and monitors latency of room operations.
/// Targets:
/// - CreateRoom: <100ms
/// - JoinRoom: <200ms
/// - SendCommand: <50ms
/// 
/// Logs warnings if operations exceed targets.
/// </summary>
public sealed class OperationLatencyTracker
{
    private readonly ILogger<OperationLatencyTracker> _logger;

    public const long CreateRoomTargetMs = 100;
    public const long JoinRoomTargetMs = 200;
    public const long SendCommandTargetMs = 50;

    public event EventHandler<LatencyMeasuredEventArgs>? LatencyMeasured;

    public OperationLatencyTracker(ILogger<OperationLatencyTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Measures latency of an async operation and logs if it exceeds target.
    /// </summary>
    public async Task<T> MeasureAsync<T>(
        string operationName,
        long targetMs,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation(cancellationToken);
            sw.Stop();

            if (sw.ElapsedMilliseconds > targetMs)
            {
                _logger.LogWarning(
                    "OperationSlowWarning {Operation} {Elapsed}ms (target: {Target}ms)",
                    operationName,
                    sw.ElapsedMilliseconds,
                    targetMs);
            }
            else
            {
                _logger.LogInformation(
                    "OperationCompleted {Operation} {Elapsed}ms",
                    operationName,
                    sw.ElapsedMilliseconds);
            }

            LatencyMeasured?.Invoke(this, new LatencyMeasuredEventArgs(operationName, sw.ElapsedMilliseconds, targetMs));
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "OperationFailed {Operation} after {Elapsed}ms", operationName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

public sealed record LatencyMeasuredEventArgs(string OperationName, long ElapsedMs, long TargetMs)
{
    public bool ExceededTarget => ElapsedMs > TargetMs;
}
