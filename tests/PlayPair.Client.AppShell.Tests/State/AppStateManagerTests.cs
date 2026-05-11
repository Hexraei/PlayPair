using Xunit;
using PlayPair.Client.AppShell.State;

namespace PlayPair.Client.AppShell.Tests.State;

public sealed class AppStateManagerTests
{
    [Fact]
    public void SetRoom_SetsAllProperties()
    {
        var manager = new AppStateManager();

        manager.SetRoom("ABC123", "client-1", SourceRole.Host, "John");

        Assert.Equal("ABC123", manager.CurrentRoomCode);
        Assert.Equal("client-1", manager.ClientId);
        Assert.Equal(SourceRole.Host, manager.CurrentRole);
        Assert.Equal("John", manager.DisplayName);
    }

    [Fact]
    public void SetRoom_NullRoomCode_Throws()
    {
        var manager = new AppStateManager();

        var ex = Assert.Throws<ArgumentNullException>(
            () => manager.SetRoom(null!, "client-1", SourceRole.Host, "John"));

        Assert.Equal("roomCode", ex.ParamName);
    }

    [Fact]
    public void UpdateConnectionState_TogglesIsConnected()
    {
        var manager = new AppStateManager();

        Assert.False(manager.IsConnected);

        manager.UpdateConnectionState(true);
        Assert.True(manager.IsConnected);

        manager.UpdateConnectionState(false);
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public void ClearRoom_ResetsAllProperties()
    {
        var manager = new AppStateManager();
        manager.SetRoom("ABC123", "client-1", SourceRole.Host, "John");
        manager.UpdateConnectionState(true);

        manager.ClearRoom();

        Assert.Null(manager.CurrentRoomCode);
        Assert.Null(manager.ClientId);
        Assert.Null(manager.DisplayName);
        Assert.Equal(SourceRole.Guest, manager.CurrentRole);
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public void StateChanged_FiredOnPropertyChanges()
    {
        var manager = new AppStateManager();
        int changeCount = 0;
        manager.StateChanged += (_, _) => changeCount++;

        manager.SetRoom("ABC123", "client-1", SourceRole.Host, "John");
        // RoomCode, ClientId, DisplayName changes (Role doesn't change since it goes from Guest to Host)
        int afterSetRoom = changeCount;
        Assert.True(afterSetRoom >= 3, $"Expected at least 3 changes after SetRoom, got {afterSetRoom}");

        manager.UpdateConnectionState(true);
        Assert.Equal(afterSetRoom + 1, changeCount);

        manager.ClearRoom();
        // All 4 room properties + connection state being reset
        Assert.True(changeCount >= afterSetRoom + 6, $"Expected at least 6 more changes after ClearRoom");
    }

    [Fact]
    public void StateChanged_NotFiredOnDuplicateValue()
    {
        var manager = new AppStateManager();
        manager.SetRoom("ABC123", "client-1", SourceRole.Host, "John");
        int changeCount = 0;
        manager.StateChanged += (_, _) => changeCount++;

        manager.UpdateConnectionState(false);
        Assert.Equal(0, changeCount);

        manager.UpdateConnectionState(true);
        Assert.Equal(1, changeCount);

        manager.UpdateConnectionState(true);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var manager = new AppStateManager();
        manager.SetRoom("ABC123", "client-1", SourceRole.Guest, "Alice");
        manager.UpdateConnectionState(true);

        string str = manager.ToString();

        Assert.Contains("ABC123", str);
        Assert.Contains("client-1", str);
        Assert.Contains("Guest", str);
        Assert.Contains("Alice", str);
        Assert.Contains("Connected=True", str);
    }
}
