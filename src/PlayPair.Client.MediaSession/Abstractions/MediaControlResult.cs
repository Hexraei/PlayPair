namespace PlayPair.Client.MediaSession.Abstractions;

public sealed record MediaControlResult(bool Succeeded, string Code, string Message)
{
    public static MediaControlResult Success(string message = "Command applied.") => new(true, "ok", message);

    public static MediaControlResult Failure(string code, string message) => new(false, code, message);
}
