using Microsoft.UI.Dispatching;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using FluentBitwarden.Contracts.Infrastructure.Settings.Models;
using FluentBitwarden.Contracts.Modules.AppState;
using WindowsClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace FluentBitwarden.Infrastructure.Clipboard;

internal static class ClipboardManager
{
    private static DispatcherQueueTimer? _clearTimer;
    private static string? _lastCopiedText;

    public static void SetText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var package = new DataPackage();
        package.SetText(text);

        var options = new ClipboardContentOptions
        {
            IsAllowedInHistory = false,
            IsRoamable = false,
        };

        WindowsClipboard.SetContentWithOptions(package, options);
        ScheduleClear(text);
    }

    public static void Clear()
    {
        StopClearTimer();
        _lastCopiedText = null;
        WindowsClipboard.Clear();
    }

    private static void ScheduleClear(string copiedText)
    {
        StopClearTimer();
        _lastCopiedText = copiedText;

        var delay = SettingsStore.Instance.Get(AppSettingKeys.Clipboard.ClearDelayKey);
        if (delay == ClipboardClearDelay.Never)
            return;

        _clearTimer = App.Current.DispatcherQueue.CreateTimer();
        _clearTimer.Interval = TimeSpan.FromSeconds((uint)delay);
        _clearTimer.IsRepeating = false;
        _clearTimer.Tick += OnClearTimerTick;
        _clearTimer.Start();
    }

    private static void StopClearTimer()
    {
        if (_clearTimer is null)
            return;

        _clearTimer.Tick -= OnClearTimerTick;
        _clearTimer.Stop();
        _clearTimer = null;
    }

    private static async void OnClearTimerTick(DispatcherQueueTimer sender, object args)
    {
        StopClearTimer();

        string? expectedText = _lastCopiedText;
        _lastCopiedText = null;
        if (expectedText is null)
            return;

        try
        {
            var content = WindowsClipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
                return;

            string currentText = await content.GetTextAsync();
            if (currentText == expectedText)
            {
                WindowsClipboard.Clear();
            }
        }
        catch (Exception)
        {
            Debug.WriteLine("Clipboard auto-clear skipped because the clipboard was unavailable.");
        }
    }
}
