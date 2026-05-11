using System.Windows;
using Microsoft.Extensions.Logging;
using PlayPair.Client.AppShell.Services;
using PlayPair.Client.AppShell.Coordination;
using PlayPair.Client.AppShell.Errors;
using PlayPair.Client.Transport;
using PlayPair.Client.AppShell.Shell;
using PlayPair.Client.AppShell.ViewModels;
using PlayPair.Client.AppShell.Views;
using PlayPair.Client.MediaSession;
using PlayPair.Contracts.Models;

namespace PlayPair.Client.AppShell;

public partial class App : System.Windows.Application
{
    private ILoggerFactory? _loggerFactory;
    private ITrayIconHost? _trayIconHost;
    private StatusOverlayWindow? _statusOverlayWindow;
    private AppShellViewModel? _viewModel;
    private SyncCoordinator? _syncCoordinator;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddDebug();
        });

        var clientLogger = _loggerFactory.CreateLogger<PlayPairClient>();
        var playPairClient = new PlayPairClient("http://localhost:5000/hubs/room", clientLogger);
        
        // Initialize media session and sync coordinator
        var mediaSessionClient = new MediaSessionClient(_loggerFactory.CreateLogger<MediaSessionClient>());
        _syncCoordinator = new SyncCoordinator(
            mediaSessionClient,
            playPairClient,
            _loggerFactory.CreateLogger<SyncCoordinator>(),
            _loggerFactory);
        
        var roomService = new SignalRRoomShellService(playPairClient, _loggerFactory.CreateLogger<SignalRRoomShellService>());
        var clipboardService = new WindowsClipboardService();
        var notificationService = new WindowsUserNotificationService();
        
        // Create operation tracking and error handling
        var latencyTracker = new OperationLatencyTracker(_loggerFactory.CreateLogger<OperationLatencyTracker>());
        var errorHandler = new ErrorHandler(_loggerFactory.CreateLogger<ErrorHandler>());

        _viewModel = new AppShellViewModel(
            roomService,
            clipboardService,
            notificationService,
            _loggerFactory.CreateLogger<AppShellViewModel>(),
            latencyTracker,
            errorHandler);

        _statusOverlayWindow = new StatusOverlayWindow
        {
            DataContext = _viewModel
        };

        _statusOverlayWindow.RequestCloseToTray += (_, _) => _statusOverlayWindow.Hide();

        _viewModel.ShowOverlayRequested += (_, _) => ShowOverlay();
        _viewModel.ExitRequested += (_, _) => ExitApplication();
        _viewModel.StateChanged += async (_, _) => 
        {
            UpdateTrayState();
            await UpdateSyncCoordinatorStateAsync();
        };

        _trayIconHost = new WindowsTrayIconHost(_loggerFactory.CreateLogger<WindowsTrayIconHost>());
        _trayIconHost.CreateRoomClicked += async (_, _) => await _viewModel.CreateRoomAsync();
        _trayIconHost.JoinRoomClicked += (_, _) => ShowOverlay();
        _trayIconHost.CopyRoomCodeClicked += async (_, _) => await _viewModel.CopyRoomCodeAsync();
        _trayIconHost.ReconnectClicked += async (_, _) => await _viewModel.ReconnectAsync();
        _trayIconHost.ExitClicked += (_, _) => ExitApplication();
        _trayIconHost.OverlayRequested += (_, _) => ShowOverlay();
        _trayIconHost.Initialize();

        UpdateTrayState();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ = ShutdownSyncCoordinatorAsync();
        _trayIconHost?.Dispose();
        _loggerFactory?.Dispose();
        base.OnExit(e);
    }

    private async Task ShutdownSyncCoordinatorAsync()
    {
        if (_syncCoordinator?.IsActive == true)
        {
            await _syncCoordinator.StopAsync(CancellationToken.None);
            await _syncCoordinator.DisposeAsync();
        }
    }

    private async Task UpdateSyncCoordinatorStateAsync()
    {
        if (_viewModel is null || _syncCoordinator is null)
        {
            return;
        }

        if (_viewModel.IsInRoom && !_syncCoordinator.IsActive)
        {
            // Room entered - start sync coordinator
            try
            {
                // Get room details from ViewModel
                var roomCode = _viewModel.CurrentRoomCode ?? string.Empty;
                if (string.IsNullOrWhiteSpace(roomCode))
                {
                    return;
                }

                // Determine the role (this would typically come from the room operation)
                // For now, we'll default to GUEST and let the sync engine determine the actual role
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _syncCoordinator.StartAsync(roomCode, "sync-client", SourceRole.GUEST, cts.Token);
            }
            catch (Exception ex)
            {
                var logger = _loggerFactory?.CreateLogger<App>();
                logger?.LogError(ex, "Failed to start sync coordinator");
            }
        }
        else if (!_viewModel.IsInRoom && _syncCoordinator.IsActive)
        {
            // Room exited - stop sync coordinator
            await _syncCoordinator.StopAsync(CancellationToken.None);
        }
    }

    private void ShowOverlay()
    {
        if (_statusOverlayWindow is null)
        {
            return;
        }

        _statusOverlayWindow.Show();
        _statusOverlayWindow.WindowState = WindowState.Normal;
        _statusOverlayWindow.Activate();
    }

    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _statusOverlayWindow?.AllowClose();
        _statusOverlayWindow?.Close();
        Shutdown();
    }

    private void UpdateTrayState()
    {
        if (_trayIconHost is null || _viewModel is null)
        {
            return;
        }

        _trayIconHost.UpdateState(_viewModel.CurrentRoomCode, _viewModel.IsConnected);
    }
}
