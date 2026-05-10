using PlayPair.Client.MediaSession.Abstractions;
using PlayPair.Client.MediaSession.Compatibility;

namespace PlayPair.Client.MediaSession.Tests.Compatibility;

public sealed class MediaSessionCompatibilityMapperTests
{
    [Fact]
    public void Map_ReturnsNotDetected_WhenSessionIsMissing()
    {
        var result = MediaSessionCompatibilityMapper.Map(false, new MediaSessionCapabilities(false, false, false));

        Assert.Equal(MediaSessionCompatibility.NotDetected, result);
    }

    [Fact]
    public void Map_ReturnsSupported_WhenAllCapabilitiesAreAvailable()
    {
        var result = MediaSessionCompatibilityMapper.Map(true, new MediaSessionCapabilities(true, true, true));

        Assert.Equal(MediaSessionCompatibility.Supported, result);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    public void Map_ReturnsPartial_WhenOnlySubsetOfCapabilitiesAreAvailable(bool canPlay, bool canPause, bool canSeek)
    {
        var result = MediaSessionCompatibilityMapper.Map(true, new MediaSessionCapabilities(canPlay, canPause, canSeek));

        Assert.Equal(MediaSessionCompatibility.Partial, result);
    }
}
