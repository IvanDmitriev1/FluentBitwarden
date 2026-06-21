using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Settings.Models;
using Windows.ApplicationModel.DataTransfer;
using WindowsClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace FluentBitwarden.Platform.Clipboard;

public static class ClipboardManager
{
    private static readonly ClipboardStaExecutor Executor = new();

    private static Timer? _clearTimer;
    private static string? _lastCopiedText;
    private static int _generation;

    public static void SetText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Executor.Invoke(() =>
        {
            var package = new DataPackage();
            package.SetText(text);

            var options = new ClipboardContentOptions
            {
                IsAllowedInHistory = false,
                IsRoamable = false,
            };

            WindowsClipboard.SetContentWithOptions(package, options);
            ScheduleClear(text);
        });
    }

    public static void Clear() => Executor.Invoke(() =>
    {
        unchecked
        {
            _generation++;
        }

        StopClearTimer();
        _lastCopiedText = null;
        WindowsClipboard.Clear();
    });

    private static void ScheduleClear(string copiedText)
    {
        StopClearTimer();
        _lastCopiedText = copiedText;
        int generation = unchecked(++_generation);

        var delay = Settings.SettingsStore.Instance.Get(AppSettingKeys.Clipboard.ClearDelayKey);
        if (delay == ClipboardClearDelay.Never)
            return;

        _clearTimer = new Timer(
            _ => Executor.Post(() => ClearIfUnchanged(generation)),
            null,
            TimeSpan.FromSeconds((uint)delay),
            Timeout.InfiniteTimeSpan);
    }

    private static void ClearIfUnchanged(int generation)
    {
        if (generation != _generation)
            return;

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

            var currentText = content.GetTextAsync().AsTask().GetAwaiter().GetResult();
            if (StringComparer.Ordinal.Equals(currentText, expectedText))
                WindowsClipboard.Clear();
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Clipboard auto-clear skipped: {exception.Message}");
        }
    }

    private static void StopClearTimer()
    {
        _clearTimer?.Dispose();
        _clearTimer = null;
    }
}