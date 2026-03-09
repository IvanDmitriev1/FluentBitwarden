using FluentBitwarden.Ui.Controls;

namespace FluentBitwarden.Ui.Abstractions;

public interface INotificationService
{
    void Initialize(NotificationHost notificationHost);

    void ShowInfo(string title, string message, TimeSpan? duration = null);
    void ShowSuccess(string title, string message, TimeSpan? duration = null);
    void ShowWarning(string title, string message, TimeSpan? duration = null);
    void ShowError(string title, string message, TimeSpan? duration = null);
}