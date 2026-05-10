using PlayPair.Client.MediaSession.Abstractions;

namespace PlayPair.Client.MediaSession.Compatibility;

public static class MediaSessionCompatibilityMapper
{
    public static MediaSessionCompatibility Map(bool sessionDetected, MediaSessionCapabilities capabilities)
    {
        if (!sessionDetected)
        {
            return MediaSessionCompatibility.NotDetected;
        }

        if (capabilities.CanPlay && capabilities.CanPause && capabilities.CanSeek)
        {
            return MediaSessionCompatibility.Supported;
        }

        return MediaSessionCompatibility.Partial;
    }
}
