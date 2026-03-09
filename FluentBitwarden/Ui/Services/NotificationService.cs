using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Controls;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Services;

internal sealed class NotificationService : INotificationService
{
    public NotificationService(TimeSpan defaultDuration)
    {
        _defaultDuration = defaultDuration;
    }

    private readonly TimeSpan _defaultDuration;
    private WeakReference<NotificationHost>? _notificationHost;

    public void Initialize(NotificationHost notificationHost)
    {
        if (_notificationHost is not null)
        {
            _notificationHost.SetTarget(notificationHost);
        }
        else
        {
            _notificationHost = new WeakReference<NotificationHost>(notificationHost);
        }
    }

    private void ShowNotification(string title, string message, InfoBarSeverity severity, TimeSpan? duration = null)
    {
        if (_notificationHost is null || !_notificationHost.TryGetTarget(out var host))
            return;

        host.QueueNotification(new NotificationMessage(
            title,
            message,
            severity,
            duration ?? _defaultDuration));
    }

    public void ShowInfo(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Informational, duration);

    public void ShowSuccess(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Success, duration);

    public void ShowWarning(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Warning, duration);

    public void ShowError(string title, string message, TimeSpan? duration = null) =>
        ShowNotification(title, message, InfoBarSeverity.Error, duration);
}