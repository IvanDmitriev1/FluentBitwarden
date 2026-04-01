using Microsoft.UI.Xaml;

namespace FluentBitwarden.Modules.AppState.Models;

public sealed record AppSettings(
    ElementTheme ThemeMode,
    bool LockOnMinimize,
    bool LockOnSystemSuspend,
    int LockTimeoutMinutes,
    int ClearClipboardAfterSeconds)
{

    public static AppSettings CreateDefault() => new(
        ThemeMode: ElementTheme.Default,
        LockOnMinimize: true,
        LockOnSystemSuspend: true,
        LockTimeoutMinutes: 10,
        ClearClipboardAfterSeconds: 2);
}
