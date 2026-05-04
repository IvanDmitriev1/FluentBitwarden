namespace FluentBitwarden.Shared.Services.Abstractions;

internal interface INotificationService
{
    void ShowInfo(string title, string message, TimeSpan? duration = null);
    void ShowSuccess(string title, string message, TimeSpan? duration = null);
    void ShowWarning(string title, string message, TimeSpan? duration = null);
    void ShowError(string title, string message, TimeSpan? duration = null);
}