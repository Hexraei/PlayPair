using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PlayPair.Client.AppShell.Services;
using PlayPair.Client.AppShell.Errors;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.AppShell.ViewModels;

public sealed class AppShellViewModel : ObservableObject
{
    private static readonly Regex RoomCodeRegex = new("^[A-Z0-9]{6}$", RegexOptions.Compiled);
    private readonly IRoomShellService _roomShellService;
    private readonly IClipboardService _clipboardService;
    private readonly IUserNotificationService _notificationService;
    private readonly ILogger<AppShellViewModel> _logger;
    private readonly OperationLatencyTracker _latencyTracker;
    private readonly ErrorHandler _errorHandler;
    private string? _currentRoomCode;
    private bool _isConnected;
    private string _connectedStatusText = "Disconnected";
    private string _mediaDetectionStatusText = "Media detection placeholder (not integrated)";
    private string _syncStateText = "Sync state placeholder (not integrated)";
    private string _joinRoomCodeInput = string.Empty;
    private string? _errorMessage;
    private bool _isInRoom;
    private bool _isBusy;
    private SourceRole _currentRole = SourceRole.GUEST;
    private string _mediaTitle = "None";
    private string _mediaApp = "None";
    private string _playbackStateText = "Unknown";
    private string _roomParticipantsStatus = "Waiting for participants...";
    private string? _transientMessage;

    public AppShellViewModel(
        IRoomShellService roomShellService,
        IClipboardService clipboardService,
        IUserNotificationService notificationService,
        ILogger<AppShellViewModel> logger,
        OperationLatencyTracker? latencyTracker = null,
        ErrorHandler? errorHandler = null)
    {
        _roomShellService = roomShellService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _logger = logger;
        _latencyTracker = latencyTracker ?? throw new ArgumentNullException(nameof(latencyTracker), "OperationLatencyTracker is required");
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler), "ErrorHandler is required");

        _errorHandler.UserFriendlyErrorOccurred += (_, args) =>
        {
            SetError(args.UserMessage);
        };

        ShowOverlayCommand = new RelayCommand(_ => ShowOverlayRequested?.Invoke(this, EventArgs.Empty));
        CreateRoomCommand = new RelayCommand(async _ => await CreateRoomAsync(), _ => !IsBusy);
        JoinRoomCommand = new RelayCommand(async _ => await JoinRoomAsync(), _ => !IsBusy);
        CopyRoomCodeCommand = new RelayCommand(async _ => await CopyRoomCodeAsync(), _ => !IsBusy);
        ReconnectCommand = new RelayCommand(async _ => await ReconnectAsync(), _ => !IsBusy);
        LeaveRoomCommand = new RelayCommand(async _ => await LeaveRoomAsync(), _ => !IsBusy);
        ClearErrorCommand = new RelayCommand(_ => ClearError());
        ExitCommand = new RelayCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? ShowOverlayRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? StateChanged;

    public ICommand ShowOverlayCommand { get; }

    public ICommand CreateRoomCommand { get; }

    public ICommand JoinRoomCommand { get; }

    public ICommand CopyRoomCodeCommand { get; }

    public ICommand ReconnectCommand { get; }

    public ICommand LeaveRoomCommand { get; }

    public ICommand ClearErrorCommand { get; }

    public ICommand ExitCommand { get; }

    public string? CurrentRoomCode
    {
        get => _currentRoomCode;
        private set
        {
            if (SetProperty(ref _currentRoomCode, value))
            {
                OnPropertyChanged(nameof(HasRoomCode));
            }
        }
    }

    public bool HasRoomCode => !string.IsNullOrWhiteSpace(CurrentRoomCode);

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public string ConnectedStatusText
    {
        get => _connectedStatusText;
        private set => SetProperty(ref _connectedStatusText, value);
    }

    public string MediaDetectionStatusText
    {
        get => _mediaDetectionStatusText;
        private set => SetProperty(ref _mediaDetectionStatusText, value);
    }

    public string SyncStateText
    {
        get => _syncStateText;
        private set => SetProperty(ref _syncStateText, value);
    }

    public SourceRole CurrentRole
    {
        get => _currentRole;
        private set => SetProperty(ref _currentRole, value);
    }

    public string MediaTitle
    {
        get => _mediaTitle;
        private set => SetProperty(ref _mediaTitle, value);
    }

    public string MediaApp
    {
        get => _mediaApp;
        private set => SetProperty(ref _mediaApp, value);
    }

    public string PlaybackStateText
    {
        get => _playbackStateText;
        private set => SetProperty(ref _playbackStateText, value);
    }

    public string RoomParticipantsStatus
    {
        get => _roomParticipantsStatus;
        private set => SetProperty(ref _roomParticipantsStatus, value);
    }

    public string? TransientMessage
    {
        get => _transientMessage;
        private set
        {
            if (SetProperty(ref _transientMessage, value))
            {
                OnPropertyChanged(nameof(HasTransientMessage));
            }
        }
    }

    public bool HasTransientMessage => !string.IsNullOrWhiteSpace(TransientMessage);

    public string JoinRoomCodeInput
    {
        get => _joinRoomCodeInput;
        set => SetProperty(ref _joinRoomCodeInput, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsInRoom
    {
        get => _isInRoom;
        private set => SetProperty(ref _isInRoom, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public async Task CreateRoomAsync()
    {
        _logger.LogInformation("UserAction {Action}", "CreateRoom");
        await ExecuteOperationAsync(
            () => _roomShellService.CreateRoomAsync(CancellationToken.None),
            "Failed to create room.");
    }

    public async Task JoinRoomAsync()
    {
        var normalizedRoomCode = (JoinRoomCodeInput ?? string.Empty).Trim().ToUpperInvariant();
        if (!RoomCodeRegex.IsMatch(normalizedRoomCode))
        {
            SetError("Room code must be 6 alphanumeric characters.");
            _logger.LogWarning("UserAction {Action} failed validation for room code input", "JoinRoom");
            return;
        }

        _logger.LogInformation("UserAction {Action} with room {RoomCode}", "JoinRoom", normalizedRoomCode);
        await ExecuteOperationAsync(
            () => _roomShellService.JoinRoomAsync(normalizedRoomCode, CancellationToken.None),
            "Failed to join room.");
    }

    public async Task CopyRoomCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentRoomCode))
        {
            SetError("No active room code to copy.");
            _logger.LogWarning("UserAction {Action} rejected because no room code is active", "CopyRoomCode");
            return;
        }

        try
        {
            await _clipboardService.SetTextAsync(CurrentRoomCode, CancellationToken.None);
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasError));
            _notificationService.ShowInfo("PlayPair", "Room code copied to clipboard.");
            _logger.LogInformation("UserAction {Action} succeeded for room {RoomCode}", "CopyRoomCode", CurrentRoomCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserAction {Action} failed", "CopyRoomCode");
            SetError("Unable to copy room code to clipboard.");
        }
    }

    public async Task ReconnectAsync()
    {
        _logger.LogInformation("UserAction {Action}", "Reconnect");
        await ExecuteOperationAsync(
            () => _roomShellService.ReconnectAsync(CancellationToken.None),
            "Failed to reconnect.");
    }

    public async Task LeaveRoomAsync()
    {
        _logger.LogInformation("UserAction {Action}", "LeaveRoom");
        await ExecuteOperationAsync(
            () => _roomShellService.LeaveRoomAsync(CancellationToken.None),
            "Failed to leave room.");
    }

    private async Task ExecuteOperationAsync(
        Func<Task<RoomOperationResult>> operation,
        string genericFailureMessage)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await operation();
            if (!result.Succeeded || result.State is null)
            {
                var errMsg = result.ErrorMessage ?? genericFailureMessage;
                if (errMsg.Contains("connection_already_in_room"))
                {
                    ShowTransientMessage("Connection is Stable.");
                    return;
                }

                SetError(CleanErrorMessage(errMsg));
                return;
            }

            ApplyState(result.State);
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasError));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shell action failed");
            var errMsg = ex.ToString();
            if (errMsg.Contains("connection_already_in_room"))
            {
                ShowTransientMessage("Connection is Stable.");
                return;
            }

            // Try to parse as hub exception if it has message payload
            if (ex.Message.StartsWith("{"))
            {
                _errorHandler.HandleHubException(ex.Message);
            }
            else
            {
                _errorHandler.HandleError(ex);
            }
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string CleanErrorMessage(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return "An unexpected error occurred.";
        }

        // Try to locate JSON inside the message
        int jsonStartIndex = rawMessage.IndexOf('{');
        if (jsonStartIndex >= 0)
        {
            string jsonPart = rawMessage.Substring(jsonStartIndex);
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonPart);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var msgProp))
                {
                    return msgProp.GetString() ?? rawMessage;
                }
                if (root.TryGetProperty("code", out var codeProp))
                {
                    string code = codeProp.GetString() ?? "";
                    return _errorHandler.FormatError(code);
                }
            }
            catch
            {
                // Fallback to text cleaning if JSON parsing fails
            }
        }

        string clean = rawMessage;
        if (clean.StartsWith("An unexpected error occurred invoking"))
        {
            int hubExceptionIndex = clean.IndexOf("HubException:");
            if (hubExceptionIndex >= 0)
            {
                clean = clean.Substring(hubExceptionIndex + "HubException:".Length).Trim();
            }
        }

        return clean;
    }

    private void ApplyState(ShellSessionState state)
    {
        CurrentRoomCode = state.RoomCode;
        IsConnected = state.IsConnected;
        ConnectedStatusText = state.IsConnected ? "Connected" : "Disconnected";
        MediaDetectionStatusText = state.MediaDetectionStatus;
        SyncStateText = state.SyncState;
        IsInRoom = state.IsInRoom;
        CurrentRole = state.Role;
    }

    public void UpdateMediaState(string title, string app, string stateText)
    {
        MediaTitle = string.IsNullOrWhiteSpace(title) ? "None" : title;
        MediaApp = string.IsNullOrWhiteSpace(app) ? "None" : app;
        PlaybackStateText = stateText;
        MediaDetectionStatusText = $"{MediaApp} - {MediaTitle} ({PlaybackStateText})";
    }

    public void UpdateParticipants(int count, string hostName, IReadOnlyList<string> guestNames)
    {
        if (count <= 1)
        {
            RoomParticipantsStatus = $"Waiting for guest (Host: {hostName})";
        }
        else
        {
            RoomParticipantsStatus = $"Connected: {string.Join(", ", guestNames)} (Host: {hostName})";
        }
        SyncStateText = $"{count} participant(s) in room";
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(HasError));
        _notificationService.ShowError("PlayPair Error", message);
    }

    public void ClearError()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(HasError));
    }

    private void RaiseCommandCanExecuteChanged()
    {
        foreach (var command in new[]
                 {
                     CreateRoomCommand,
                     JoinRoomCommand,
                     CopyRoomCodeCommand,
                     ReconnectCommand,
                     LeaveRoomCommand
                 })
        {
            if (command is RelayCommand relayCommand)
            {
                relayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public void ShowTransientMessage(string message)
    {
        TransientMessage = message;
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                TransientMessage = null;
            });
        });
    }
}

