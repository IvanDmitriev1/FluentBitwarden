using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.Platform.Infrastructure.Clipboard;

internal sealed class WindowsClipboardManager : IClipboardManager
{
    public void SetText(string? text, ClipboardClearDelay delay) => ClipboardManager.SetText(text, delay);

    public void Clear() => ClipboardManager.Clear();
}
