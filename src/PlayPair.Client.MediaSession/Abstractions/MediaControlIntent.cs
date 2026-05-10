namespace PlayPair.Client.MediaSession.Abstractions;

public enum MediaControlIntentType
{
    Play,
    Pause,
    Seek
}

public sealed record MediaControlIntent(MediaControlIntentType Type, TimeSpan? SeekPosition = null);
