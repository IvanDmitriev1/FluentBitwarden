using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Abstractions;

public sealed record NotificationMessage(
    string Title,
    string Message,
    InfoBarSeverity Severity,
    TimeSpan Duration);