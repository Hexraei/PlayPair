namespace PlayPair.Client.AppShell.Services;

public interface IUserNotificationService
{
    void ShowInfo(string title, string message);

    void ShowError(string title, string message);
}
