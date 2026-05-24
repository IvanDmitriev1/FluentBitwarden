namespace FluentBitwarden.Infrastructure.Abstractions;

internal sealed class NotificationService : INotificationService
{
    public void ShowInfo(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Informational, duration);

    public void ShowSuccess(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Success, duration);

    public void ShowWarning(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Warning, duration);

    public void ShowError(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Error, duration);

    private void ShowNotification(string title, string message, InfoBarSeverity severity, TimeSpan? duration = null)
    {

    }
}