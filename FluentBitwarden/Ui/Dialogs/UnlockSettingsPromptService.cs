using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Dialogs;

internal sealed class UnlockSettingsPromptService(MainWindow mainWindow)
    : IUnlockSettingsPromptService
{
    public async ValueTask<bool> ShowUnlockSettingsPromptAsync(CancellationToken cancellationToken = default)
    {
        if (mainWindow.Content is not FrameworkElement root || root.XamlRoot is null)
        {
            return false;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = root.XamlRoot,
            Title = "Set up faster unlock?",
            Content = "Enable Windows Hello or an app PIN in Settings so you can unlock the vault faster next time.",
            PrimaryButtonText = "Open settings",
            CloseButtonText = "Later",
            DefaultButton = ContentDialogButton.Primary,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
