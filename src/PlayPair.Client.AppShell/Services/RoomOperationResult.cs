namespace PlayPair.Client.AppShell.Services;

public sealed record RoomOperationResult(
    bool Succeeded,
    string? ErrorMessage,
    ShellSessionState? State)
{
    public static RoomOperationResult Success(ShellSessionState state) => new(true, null, state);

    public static RoomOperationResult Failure(string errorMessage) => new(false, errorMessage, null);
}
