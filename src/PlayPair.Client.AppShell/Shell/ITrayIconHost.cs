namespace PlayPair.Client.AppShell.Shell;

public interface ITrayIconHost : IDisposable
{
    event EventHandler? OverlayRequested;
    event EventHandler? CreateRoomClicked;
    event EventHandler? JoinRoomClicked;
    event EventHandler? CopyRoomCodeClicked;
    event EventHandler? ReconnectClicked;
    event EventHandler? ExitClicked;

    void Initialize();

    void UpdateState(string? roomCode, bool isConnected);
}
