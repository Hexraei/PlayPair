namespace PlayPair.Client.MediaSession.Platform;

internal interface ISystemMediaSessionManager
{
    event EventHandler? CurrentSessionChanged;

    Task<bool> InitializeAsync(CancellationToken cancellationToken);

    ISystemMediaSession? GetCurrentSession();
}
