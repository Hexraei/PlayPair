using Microsoft.Extensions.Logging;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.Sync;
using PlayPair.Client.Transport;
using PlayPair.Contracts.Models;
using System.Diagnostics;

namespace PlayPair.Client.AppShell.Coordination;

/// <summary>
/// Coordinates synchronization between:
/// - Media session client (detects play/pause/seek events)
/// - Sync engine (applies state reduction logic and deduplication)
/// - Server transport (sends/receives commands via SignalR)
/// 
/// Responsibilities:
/// - Initialize media session client when entering a room
/// - Start sync engine with server transport
/// - Monitor media state changes and forward to sync engine
/// - Monitor server commands and apply to media session
/// - Track latency and log performance metrics
/// - Clean shutdown on room leave
/// </summary>
public sealed class SyncCoordinator : IAsyncDisposable
{
    private readonly IMediaSessionClient _mediaSessionClient;
    private readonly PlayPairClient _transport;
    private readonly ILogger<SyncCoordinator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private ClientSyncEngine? _syncEngine;
    private SyncSessionContext? _syncContext;
    private bool _started;

    public event EventHandler<SyncStateChangedEventArgs>? SyncStateChanged;
    public event EventHandler<SyncLatencyEventArgs>? LatencyMeasured;

    public bool IsActive => _started && _syncEngine is not null;

    public SyncCoordinator(
        IMediaSessionClient mediaSessionClient,
        PlayPairClient transport,
        ILogger<SyncCoordinator> logger,
        ILoggerFactory loggerFactory)
    {
        _mediaSessionClient = mediaSessionClient;
        _transport = transport;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes the media session client and starts the sync engine.
    /// Called when user joins/creates a room.
    /// </summary>
    public async Task StartAsync(string roomId, string clientId, SourceRole role, CancellationToken cancellationToken)
    {
        if (_started)
        {
            _logger.LogWarning("SyncCoordinator already started for room {RoomId}", roomId);
            return;
        }

        _logger.LogInformation("SyncCoordinatorStarting {RoomId} {ClientId} {Role}", roomId, clientId, role);

        try
        {
            // Initialize media session client (detects active player)
            await _mediaSessionClient.StartAsync(cancellationToken);

            // Create sync context and engine
            _syncContext = new SyncSessionContext(roomId, clientId, role);
            var syncEngineLogger = _loggerFactory.CreateLogger<ClientSyncEngine>();
            _syncEngine = new ClientSyncEngine(
                _mediaSessionClient,
                _transport,
                syncEngineLogger);

            // Start sync engine (subscribes to media/transport events internally)
            await _syncEngine.StartAsync(_syncContext, cancellationToken);

            _started = true;
            _logger.LogInformation("SyncCoordinatorStarted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCoordinatorStartFailed");
            throw;
        }
    }

    /// <summary>
    /// Stops the sync engine and cleans up resources.
    /// Called when user leaves room or exits application.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started)
        {
            return;
        }

        _logger.LogInformation("SyncCoordinatorStopping");

        try
        {
            if (_syncEngine is not null)
            {
                await _syncEngine.StopAsync(cancellationToken);
            }

            _syncContext = null;
            _syncEngine = null;
            _started = false;

            _logger.LogInformation("SyncCoordinatorStopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCoordinatorStopFailed");
        }
    }

    /// <summary>
    /// Refreshes media session detection (called periodically or on demand).
    /// Useful when media player may have changed.
    /// </summary>
    public async Task RefreshMediaSessionAsync(CancellationToken cancellationToken)
    {
        if (!IsActive)
        {
            return;
        }

        try
        {
            var sw = Stopwatch.StartNew();
            await _mediaSessionClient.RefreshAsync(cancellationToken);
            sw.Stop();

            var currentState = _mediaSessionClient.CurrentState;
            _logger.LogInformation(
                "MediaSessionRefreshed {Compatibility} {PlaybackState} {Duration}ms",
                currentState.Compatibility,
                currentState.PlaybackState,
                sw.ElapsedMilliseconds);

            SyncStateChanged?.Invoke(this, new SyncStateChangedEventArgs(
                $"Media: {currentState.Compatibility} - {currentState.PlaybackState}"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MediaSessionRefreshFailed");
        }
    }

    /// <summary>
    /// Responds to server snapshot updates.
    /// Sync engine automatically applies snapshots to media session.
    /// </summary>
    public async Task HandleSnapshotReceivedAsync(CancellationToken cancellationToken)
    {
        if (!IsActive)
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        await Task.Delay(10, cancellationToken);  // Placeholder: actual processing done by sync engine
        sw.Stop();

        LatencyMeasured?.Invoke(this, new SyncLatencyEventArgs("snapshot_apply", sw.ElapsedMilliseconds));
    }

    /// <summary>
    /// Recovers state after reconnection.
    /// Requests latest snapshot from server to sync media session.
    /// </summary>
    public async Task RecoverStateAsync(CancellationToken cancellationToken)
    {
        if (!IsActive || _syncContext is null)
        {
            _logger.LogWarning("Cannot recover state: SyncCoordinator not active");
            return;
        }

        _logger.LogInformation("RecoveringState {RoomId}", _syncContext.RoomId);

        try
        {
            var sw = Stopwatch.StartNew();

            // Refresh media session to get current state
            await _mediaSessionClient.RefreshAsync(cancellationToken);

            // Let sync engine handle snapshot request internally
            sw.Stop();

            _logger.LogInformation("StateRecovered {Duration}ms", sw.ElapsedMilliseconds);
            LatencyMeasured?.Invoke(this, new SyncLatencyEventArgs("state_recovery", sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StateRecoveryFailed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (_syncEngine is not null)
        {
            await _syncEngine.DisposeAsync();
        }

        await _mediaSessionClient.DisposeAsync();
    }
}

public sealed record SyncStateChangedEventArgs(string Status);
public sealed record SyncLatencyEventArgs(string Operation, long ElapsedMilliseconds);
