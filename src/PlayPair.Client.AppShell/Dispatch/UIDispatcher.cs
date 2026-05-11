using Microsoft.Extensions.Logging;
using PlayPair.Contracts.Models;
using System;
using System.Threading;
using System.Windows.Forms;

namespace PlayPair.Client.AppShell.Dispatch;

public sealed class UIDispatcher
{
    private readonly Control? _uiThreadControl;
    private readonly ILogger<UIDispatcher> _logger;

    public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
    public event EventHandler<SnapshotReceivedEventArgs>? SnapshotReceived;
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    public UIDispatcher(ILogger<UIDispatcher> logger, Control? uiThreadControl = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiThreadControl = uiThreadControl;
    }

    public void DispatchCommand(RoomCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _logger.LogDebug("Dispatching command: {CommandType} for room {RoomId}", command.Type, command.RoomId);

        MarshalToUIThread(() =>
        {
            CommandReceived?.Invoke(this, new CommandReceivedEventArgs(command));
        });
    }

    public void DispatchSnapshot(RoomSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        _logger.LogDebug("Dispatching snapshot for room {RoomId}", snapshot.RoomId);

        MarshalToUIThread(() =>
        {
            SnapshotReceived?.Invoke(this, new SnapshotReceivedEventArgs(snapshot));
        });
    }

    public void DispatchError(string errorCode, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentNullException(nameof(errorCode));

        _logger.LogWarning("Dispatching error: {ErrorCode} - {ErrorMessage}", errorCode, errorMessage);

        MarshalToUIThread(() =>
        {
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(errorCode, errorMessage));
        });
    }

    private void MarshalToUIThread(Action action)
    {
        if (_uiThreadControl != null && _uiThreadControl.InvokeRequired)
        {
            _uiThreadControl.Invoke(action);
        }
        else
        {
            action();
        }
    }
}

public sealed class CommandReceivedEventArgs : EventArgs
{
    public RoomCommand Command { get; }

    public CommandReceivedEventArgs(RoomCommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }
}

public sealed class SnapshotReceivedEventArgs : EventArgs
{
    public RoomSnapshot Snapshot { get; }

    public SnapshotReceivedEventArgs(RoomSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }
}

public sealed class ErrorOccurredEventArgs : EventArgs
{
    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    public ErrorOccurredEventArgs(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }
}
