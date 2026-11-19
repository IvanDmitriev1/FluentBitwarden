using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.Ui.Behaviours;

internal static class CopyTextCommand
{
    public static bool CanExecute(object? parameter) => parameter is not null;

    public static void Execute(object? parameter, IClipboardManager clipboardManager, ISettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        string text = parameter as string ?? parameter.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

        clipboardManager.SetText(text, settingsStore.Get(AppSettingKeys.Clipboard.ClearDelayKey));
    }
}
