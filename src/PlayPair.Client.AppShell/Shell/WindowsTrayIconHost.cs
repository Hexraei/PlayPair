using System.Drawing;
using Microsoft.Extensions.Logging;
using Forms = System.Windows.Forms;

namespace PlayPair.Client.AppShell.Shell;

public sealed class WindowsTrayIconHost(ILogger<WindowsTrayIconHost> logger) : ITrayIconHost
{
    private readonly ILogger<WindowsTrayIconHost> _logger = logger;
    private readonly Forms.NotifyIcon _notifyIcon = new();
    private readonly Forms.ToolStripMenuItem _createRoomMenuItem = new("&Create Room");
    private readonly Forms.ToolStripMenuItem _joinRoomMenuItem = new("&Join Room");
    private readonly Forms.ToolStripMenuItem _copyRoomCodeMenuItem = new("C&opy Room Code");
    private readonly Forms.ToolStripMenuItem _reconnectMenuItem = new("&Reconnect");
    private readonly Forms.ToolStripMenuItem _exitMenuItem = new("E&xit");
    private bool _isInitialized;

    public event EventHandler? OverlayRequested;

    public event EventHandler? CreateRoomClicked;

    public event EventHandler? JoinRoomClicked;

    public event EventHandler? CopyRoomCodeClicked;

    public event EventHandler? ReconnectClicked;

    public event EventHandler? ExitClicked;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.AddRange(
        [
            _createRoomMenuItem,
            _joinRoomMenuItem,
            _copyRoomCodeMenuItem,
            _reconnectMenuItem,
            new Forms.ToolStripSeparator(),
            _exitMenuItem
        ]);

        _createRoomMenuItem.Click += (_, _) => CreateRoomClicked?.Invoke(this, EventArgs.Empty);
        _joinRoomMenuItem.Click += (_, _) => JoinRoomClicked?.Invoke(this, EventArgs.Empty);
        _copyRoomCodeMenuItem.Click += (_, _) => CopyRoomCodeClicked?.Invoke(this, EventArgs.Empty);
        _reconnectMenuItem.Click += (_, _) => ReconnectClicked?.Invoke(this, EventArgs.Empty);
        _exitMenuItem.Click += (_, _) => ExitClicked?.Invoke(this, EventArgs.Empty);

        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "PlayPair";
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += (_, _) => OverlayRequested?.Invoke(this, EventArgs.Empty);

        _isInitialized = true;
        _logger.LogInformation("Tray icon initialized");
    }

    public void UpdateState(string? roomCode, bool isConnected)
    {
        _copyRoomCodeMenuItem.Enabled = !string.IsNullOrWhiteSpace(roomCode);
        _reconnectMenuItem.Enabled = !string.IsNullOrWhiteSpace(roomCode) && !isConnected;

        _notifyIcon.Text = string.IsNullOrWhiteSpace(roomCode)
            ? "PlayPair - Not in a room"
            : $"PlayPair - Room {roomCode} ({(isConnected ? "Connected" : "Disconnected")})";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
