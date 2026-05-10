namespace PlayPair.Client.AppShell.Services;

public interface IClipboardService
{
    Task SetTextAsync(string value, CancellationToken cancellationToken);
}
