using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;

internal interface IDialogHost
{
    XamlRoot XamlRoot { get; }
}
