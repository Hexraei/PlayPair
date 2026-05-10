using System.Windows;
using Microsoft.Extensions.Logging;
using PlayPair.Client.AppShell.Services;
using PlayPair.Client.AppShell.Shell;
using PlayPair.Client.AppShell.ViewModels;
using PlayPair.Client.AppShell.Views;

namespace PlayPair.Client.AppShell;

public partial class App : Application
{
    private ILoggerFactory? _loggerFactory;
    private ITrayIconHost? _trayIconHost;
    private StatusOverlayWindow? _statusOverlayWindow;
    private AppShellViewModel? _viewModel;
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

        var roomService = new StubRoomShellService(_loggerFactory.CreateLogger<StubRoomShellService>());
        var clipboardService = new WindowsClipboardService();
        var notificationService = new WindowsUserNotificationService();

        _viewModel = new AppShellViewModel(
            roomService,
            clipboardService,
            notificationService,
            _loggerFactory.CreateLogger<AppShellViewModel>());

        _statusOverlayWindow = new StatusOverlayWindow
        {
            DataContext = _viewModel
        };

        _statusOverlayWindow.RequestCloseToTray += (_, _) => _statusOverlayWindow.Hide();

        _viewModel.ShowOverlayRequested += (_, _) => ShowOverlay();
        _viewModel.ExitRequested += (_, _) => ExitApplication();
        _viewModel.StateChanged += (_, _) => UpdateTrayState();

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
        _trayIconHost?.Dispose();
        _loggerFactory?.Dispose();
        base.OnExit(e);
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
