using System.Windows;

namespace PlayPair.Client.AppShell.Services;

public sealed class WindowsClipboardService : IClipboardService
{
    public Task SetTextAsync(string value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clipboard.SetText(value);
        return Task.CompletedTask;
    }
}
