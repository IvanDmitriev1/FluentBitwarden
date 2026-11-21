using System.Windows.Input;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.Ui.Behaviours;

public sealed partial class CopyTextCommand : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => parameter is not null;

    public void Execute(object? parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        string text = parameter as string ?? parameter.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var clipboardManager = App.Current.GetRequiredService<IClipboardManager>();
        var settingsStore = App.Current.GetRequiredService<ISettingsStore>();
        clipboardManager.SetText(text, settingsStore.Get(AppSettingKeys.Clipboard.ClearDelayKey));
    }
}
