namespace PlayPair.Client.MediaSession.Abstractions;

public interface IMediaSessionClient : IAsyncDisposable
{
    event EventHandler<MediaSessionStateChangedEventArgs>? StateChanged;

    MediaSessionState CurrentState { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task RefreshAsync(CancellationToken cancellationToken);

    Task<MediaControlResult> ApplyIntentAsync(MediaControlIntent intent, CancellationToken cancellationToken);
}
