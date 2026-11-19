using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.Platform.Infrastructure.Clipboard;

public interface IClipboardManager
{
    void SetText(string? text, ClipboardClearDelay delay);

    void Clear();
}
