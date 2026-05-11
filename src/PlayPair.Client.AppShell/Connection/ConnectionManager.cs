using Microsoft.Extensions.Logging;
using PlayPair.Client.Transport;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlayPair.Client.AppShell.Connection;

public sealed class ConnectionManager : IAsyncDisposable
{
    private readonly PlayPairClient _client;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly Stopwatch _healthCheckStopwatch = new();
    private CancellationTokenSource? _cts;
    private Task? _healthCheckTask;
    private bool _disposed;

    public const int HealthCheckIntervalMs = 30000;
    public const int HealthCheckTimeoutMs = 5000;
    public const int MaxReconnectAttempts = 10;
    public const int InitialBackoffMs = 1000;
    public const int MaxBackoffMs = 30000;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public bool IsConnected { get; private set; }

    public bool IsInitialized { get; private set; }

    public ConnectionManager(PlayPairClient client, ILogger<ConnectionManager> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _cts = new CancellationTokenSource();
        IsInitialized = true;
        _healthCheckTask = RunHealthCheckLoopAsync(_cts.Token);
        _logger.LogInformation("ConnectionManager initialized");
        return Task.CompletedTask;
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Attempting reconnect");

        int attempt = 0;
        int backoffMs = InitialBackoffMs;

        while (attempt < MaxReconnectAttempts)
        {
            try
            {
                _logger.LogInformation("Reconnect attempt {Attempt}/{MaxAttempts}, backoff={BackoffMs}ms", 
                    attempt + 1, MaxReconnectAttempts, backoffMs);

                await Task.Delay(backoffMs, cancellationToken);

                // Health check during reconnect
                if (await CheckHealthAsync(cancellationToken))
                {
                    OnConnectionStateChanged(true);
                    _logger.LogInformation("Reconnect successful after {Attempts} attempts", attempt + 1);
                    return;
                }

                attempt++;
                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Reconnect cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnect attempt {Attempt} failed", attempt + 1);
                attempt++;
            }
        }

        _logger.LogError("Reconnect failed after {MaxAttempts} attempts; user must manually reconnect", MaxReconnectAttempts);
        OnConnectionStateChanged(false);
    }

    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            // Placeholder: actual health check would call /health endpoint
            _logger.LogDebug("Health check: OK");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
            return Task.FromResult(false);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            if (IsConnected)
            {
                await _client.LeaveRoomAsync(cancellationToken);
                OnConnectionStateChanged(false);
            }

            IsInitialized = false;
            _cts?.Cancel();
            if (_healthCheckTask is not null)
            {
                try
                {
                    await _healthCheckTask;
                }
                catch (OperationCanceledException) { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect");
        }
    }

    private async Task RunHealthCheckLoopAsync(CancellationToken cancellationToken)
    {
        int failCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _healthCheckStopwatch.Restart();
                await Task.Delay(HealthCheckIntervalMs, cancellationToken);

                bool isHealthy = await CheckHealthAsync(cancellationToken);
                long elapsedMs = _healthCheckStopwatch.ElapsedMilliseconds;

                if (isHealthy)
                {
                    failCount = 0;
                    if (!IsConnected)
                    {
                        OnConnectionStateChanged(true);
                    }

                    _logger.LogDebug("Health check passed in {ElapsedMs}ms", elapsedMs);
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("Health check failed ({FailCount}/{Threshold})", failCount, 3);

                    if (failCount >= 3)
                    {
                        OnConnectionStateChanged(false);
                        _logger.LogError("Health check threshold reached; connection marked stale");
                        failCount = 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Health check loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in health check loop");
            }
        }
    }

    private void OnConnectionStateChanged(bool isConnected)
    {
        if (IsConnected != isConnected)
        {
            IsConnected = isConnected;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(isConnected));
            _logger.LogInformation("Connection state changed: {State}", isConnected ? "Connected" : "Disconnected");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionManager));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_healthCheckTask != null)
            {
                try
                {
                    await _healthCheckTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _logger.LogInformation("ConnectionManager disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ConnectionManager disposal");
        }
    }
}

public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }

    public ConnectionStateChangedEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }
}
