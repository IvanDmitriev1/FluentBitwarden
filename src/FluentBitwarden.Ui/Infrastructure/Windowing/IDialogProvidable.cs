using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Infrastructure.Windowing;

internal interface IDialogProvidable
{
    XamlRoot XamlRoot { get; }
}
