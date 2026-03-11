using FluentBitwarden.Ui.Controls;

namespace FluentBitwarden.Ui.Abstractions;

/// <summary>
/// Shows transient in-app notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Initializes notifications with the active host control.
    /// </summary>
    void Initialize(NotificationHost notificationHost);

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    void ShowInfo(string title, string message, TimeSpan? duration = null);

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    void ShowSuccess(string title, string message, TimeSpan? duration = null);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    void ShowWarning(string title, string message, TimeSpan? duration = null);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    void ShowError(string title, string message, TimeSpan? duration = null);
}
