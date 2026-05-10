using Microsoft.Extensions.Logging.Abstractions;
using PlayPair.Client.AppShell.Services;
using PlayPair.Client.AppShell.ViewModels;

namespace PlayPair.Client.AppShell.Tests;

public class AppShellViewModelTests
{
    [Fact]
    public async Task JoinRoomAsync_WithInvalidRoomCode_SetsValidationError()
    {
        var service = new FakeRoomShellService();
        var viewModel = CreateViewModel(service);
        viewModel.JoinRoomCodeInput = "abc";

        await viewModel.JoinRoomAsync();

        Assert.Equal("Room code must be 6 alphanumeric characters.", viewModel.ErrorMessage);
        Assert.Equal(0, service.JoinCalls);
    }

    [Fact]
    public async Task CreateRoomAsync_Success_UpdatesRoomState()
    {
        var service = new FakeRoomShellService
        {
            CreateResult = RoomOperationResult.Success(new ShellSessionState("AB12CD", true, "Media placeholder", "Sync placeholder", true))
        };
        var viewModel = CreateViewModel(service);

        await viewModel.CreateRoomAsync();

        Assert.Equal("AB12CD", viewModel.CurrentRoomCode);
        Assert.Equal("Connected", viewModel.ConnectedStatusText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ReconnectAsync_ServiceFailure_ShowsError()
    {
        var service = new FakeRoomShellService
        {
            ReconnectResult = RoomOperationResult.Failure("Cannot reconnect because no room is active.")
        };
        var notifications = new TestNotificationService();
        var viewModel = CreateViewModel(service, notifications: notifications);

        await viewModel.ReconnectAsync();

        Assert.Equal("Cannot reconnect because no room is active.", viewModel.ErrorMessage);
        Assert.Contains("Cannot reconnect", notifications.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopyRoomCodeAsync_WithoutRoomCode_ShowsError()
    {
        var viewModel = CreateViewModel(new FakeRoomShellService());

        await viewModel.CopyRoomCodeAsync();

        Assert.Equal("No active room code to copy.", viewModel.ErrorMessage);
    }

    private static AppShellViewModel CreateViewModel(
        FakeRoomShellService service,
        TestClipboardService? clipboard = null,
        TestNotificationService? notifications = null)
    {
        return new AppShellViewModel(
            service,
            clipboard ?? new TestClipboardService(),
            notifications ?? new TestNotificationService(),
            NullLogger<AppShellViewModel>.Instance);
    }

    private sealed class FakeRoomShellService : IRoomShellService
    {
        public int JoinCalls { get; private set; }

        public RoomOperationResult CreateResult { get; set; } =
            RoomOperationResult.Success(new ShellSessionState("ROOM01", true, "Media placeholder", "Sync placeholder", true));

        public RoomOperationResult JoinResult { get; set; } =
            RoomOperationResult.Success(new ShellSessionState("ROOM01", true, "Media placeholder", "Sync placeholder", true));

        public RoomOperationResult ReconnectResult { get; set; } =
            RoomOperationResult.Success(new ShellSessionState("ROOM01", true, "Media placeholder", "Sync placeholder", true));

        public RoomOperationResult LeaveResult { get; set; } =
            RoomOperationResult.Success(new ShellSessionState(null, false, "Media placeholder", "Sync placeholder", false));

        public Task<RoomOperationResult> CreateRoomAsync(CancellationToken cancellationToken) => Task.FromResult(CreateResult);

        public Task<RoomOperationResult> JoinRoomAsync(string roomCode, CancellationToken cancellationToken)
        {
            JoinCalls++;
            return Task.FromResult(JoinResult);
        }

        public Task<RoomOperationResult> ReconnectAsync(CancellationToken cancellationToken) => Task.FromResult(ReconnectResult);

        public Task<RoomOperationResult> LeaveRoomAsync(CancellationToken cancellationToken) => Task.FromResult(LeaveResult);
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public string? LastCopied { get; private set; }

        public Task SetTextAsync(string value, CancellationToken cancellationToken)
        {
            LastCopied = value;
            return Task.CompletedTask;
        }
    }

    private sealed class TestNotificationService : IUserNotificationService
    {
        public string LastInfoMessage { get; private set; } = string.Empty;

        public string LastErrorMessage { get; private set; } = string.Empty;

        public void ShowInfo(string title, string message) => LastInfoMessage = message;

        public void ShowError(string title, string message) => LastErrorMessage = message;
    }
}
