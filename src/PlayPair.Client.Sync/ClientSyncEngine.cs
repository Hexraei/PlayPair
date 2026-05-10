using Microsoft.Extensions.Logging;
using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.Sync.Abstractions;
using PlayPair.Client.Sync.Reducer;
using PlayPair.Client.Sync.Transport;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync;

public sealed class ClientSyncEngine : IAsyncDisposable
{
    private readonly IMediaSessionClient _mediaSessionClient;
    private readonly IRoomSyncTransport _transport;
    private readonly ILogger<ClientSyncEngine> _logger;
    private readonly SyncEngineOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SyncReducerState? _state;
    private bool _started;

    public ClientSyncEngine(
        IMediaSessionClient mediaSessionClient,
        IRoomSyncTransport transport,
        ILogger<ClientSyncEngine> logger,
        SyncEngineOptions? options = null)
    {
        _mediaSessionClient = mediaSessionClient;
        _transport = transport;
        _logger = logger;
        _options = options ?? new SyncEngineOptions();
    }

    public async Task StartAsync(SyncSessionContext context, CancellationToken cancellationToken)
    {
        if (_started)
        {
            throw new InvalidOperationException("Sync engine has already started.");
        }

        if (string.IsNullOrWhiteSpace(context.RoomId) || string.IsNullOrWhiteSpace(context.ClientId))
        {
            throw new ArgumentException("RoomId and ClientId are required.", nameof(context));
        }

        _state = new SyncReducerState(context);
        _transport.CommandReceived += OnRemoteCommandReceived;
        _transport.SnapshotReceived += OnRemoteSnapshotReceived;
        _mediaSessionClient.StateChanged += OnMediaStateChanged;
        _started = true;

        _logger.LogInformation(
            "ClientSyncEngineStarted {RoomId} {ClientId} {Role}",
            context.RoomId,
            context.ClientId,
            context.Role);

        await _mediaSessionClient.RefreshAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _started = false;
            _mediaSessionClient.StateChanged -= OnMediaStateChanged;
            _transport.CommandReceived -= OnRemoteCommandReceived;
            _transport.SnapshotReceived -= OnRemoteSnapshotReceived;
            _state = null;
        }
        finally
        {
            _gate.Release();
        }

        _logger.LogInformation("ClientSyncEngineStopped");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _gate.Dispose();
    }

    private void OnMediaStateChanged(object? sender, MediaSessionStateChangedEventArgs args)
    {
        _ = ProcessLocalStateChangedAsync(args.State, CancellationToken.None);
    }

    private void OnRemoteCommandReceived(object? sender, RoomCommandReceivedEventArgs args)
    {
        _ = ProcessRemoteCommandAsync(args.Command, CancellationToken.None);
    }

    private void OnRemoteSnapshotReceived(object? sender, RoomSnapshotReceivedEventArgs args)
    {
        _ = ProcessSnapshotAsync(args.Snapshot, CancellationToken.None);
    }

    private async Task ProcessLocalStateChangedAsync(MediaSessionState state, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_state is null)
            {
                return;
            }

            var observed = SyncTransportAdapter.ToObservedState(state);
            var decision = SyncStateReducer.ReduceLocalObservation(_state, observed, _options);
            _state = decision.NextState;

            if (decision.ShouldPublishLocalCommand && _state.LastLocalState is not null)
            {
                var command = SyncTransportAdapter.BuildRoomCommand(_state.Context, decision.LocalCommandOperation, _state.LastLocalState);
                try
                {
                    await _transport.SendCommandAsync(command, cancellationToken);
                    _state = SyncStateReducer.RegisterLocalCommandEmission(_state, command.CommandId, _options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LocalCommandSendFailed {Operation} {CommandId}", decision.LocalCommandOperation, command.CommandId);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ProcessRemoteCommandAsync(RoomCommand command, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_state is null)
            {
                return;
            }

            var decision = SyncStateReducer.ReduceRemoteCommand(_state, command, _options);
            _state = decision.NextState;
            LogDecision(command.Type.ToString(), decision);

            if (decision.ShouldApplyRemoteIntent)
            {
                var intent = SyncTransportAdapter.ToMediaIntent(command, decision.RemoteIntentOperation);
                if (intent is null)
                {
                    _logger.LogWarning("RemoteCommandPayloadInvalid {CommandType} {CommandId}", command.Type, command.CommandId);
                }
                else
                {
                    var result = await _mediaSessionClient.ApplyIntentAsync(intent, cancellationToken);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("RemoteIntentApplyFailed {Code} {Message}", result.Code, result.Message);
                    }
                }
            }

            if (decision.ShouldRequestSnapshot && _state is not null)
            {
                await RequestAndApplySnapshotAsync(_state.Context.RoomId, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ProcessSnapshotAsync(RoomSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_state is null)
            {
                return;
            }

            var decision = SyncStateReducer.ReduceSnapshot(_state, snapshot, _options);
            _state = decision.NextState;
            LogDecision("snapshot", decision);

            if (decision.ShouldApplySnapshot)
            {
                foreach (var intent in SyncTransportAdapter.ToSnapshotIntents(snapshot, _state.LastLocalState, _options))
                {
                    var result = await _mediaSessionClient.ApplyIntentAsync(intent, cancellationToken);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("SnapshotIntentApplyFailed {Code} {Message}", result.Code, result.Message);
                    }
                }
            }

            if (decision.ShouldRequestSnapshot && _state is not null)
            {
                await RequestAndApplySnapshotAsync(_state.Context.RoomId, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RequestAndApplySnapshotAsync(string roomId, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _transport.RequestSnapshotAsync(roomId, cancellationToken);
            var nestedDecision = SyncStateReducer.ReduceSnapshot(_state!, snapshot, _options);
            _state = nestedDecision.NextState;

            if (nestedDecision.ShouldApplySnapshot)
            {
                foreach (var intent in SyncTransportAdapter.ToSnapshotIntents(snapshot, _state.LastLocalState, _options))
                {
                    var result = await _mediaSessionClient.ApplyIntentAsync(intent, cancellationToken);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("ResnapshotIntentApplyFailed {Code} {Message}", result.Code, result.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SnapshotRequestFailed {RoomId}", roomId);
        }
    }

    private void LogDecision(string source, SyncReductionDecision decision)
    {
        if (decision.ShouldLogWarning)
        {
            _logger.LogWarning("SyncDecisionWarning {Source} {Reason}", source, decision.Reason);
            return;
        }

        _logger.LogInformation("SyncDecision {Source} {Reason}", source, decision.Reason);
    }
}
