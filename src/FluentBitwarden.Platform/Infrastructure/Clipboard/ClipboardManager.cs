using FluentBitwarden.Contracts.Settings.Models;
using Windows.ApplicationModel.DataTransfer;
using WindowsClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace FluentBitwarden.Platform.Infrastructure.Clipboard;

public static class ClipboardManager
{
    private static readonly ClipboardStaExecutor Executor = new();

    private static Timer? _clearTimer;
    private static string? _lastCopiedText;
    private static int _generation;

    public static void SetText(string? text, ClipboardClearDelay delay)
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

            if (!WindowsClipboard.SetContentWithOptions(package, options))
            {
                Debug.WriteLine("Clipboard write failed; auto-clear state unchanged.");
                return;
            }

            StopClearTimer();
            int generation = unchecked(++_generation);
            if (delay == ClipboardClearDelay.Never)
            {
                _lastCopiedText = null;
                return;
            }

            _lastCopiedText = text;
            _clearTimer = new Timer(
                _ => Executor.Post(() => ClearIfUnchanged(generation)),
                null,
                TimeSpan.FromSeconds((uint)delay),
                Timeout.InfiniteTimeSpan);
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; a timer-driven best-effort clipboard clear must not throw on the timer thread.")]
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
