using Windows.Media.Control;

namespace PlayPair.Client.MediaSession.Platform;

internal sealed class WindowsSystemMediaSessionManager : ISystemMediaSessionManager
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public event EventHandler? CurrentSessionChanged;

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cancellationToken);
            _manager.CurrentSessionChanged += ManagerOnCurrentSessionChanged;
            return true;
        }
        catch (Exception)
        {
            _manager = null;
            return false;
        }
    }

    public ISystemMediaSession? GetCurrentSession()
    {
        if (_manager is null)
        {
            return null;
        }

        var session = _manager.GetCurrentSession();
        return session is null ? null : new WindowsSystemMediaSession(session);
    }

    private void ManagerOnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        CurrentSessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
