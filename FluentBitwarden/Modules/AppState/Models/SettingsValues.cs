namespace FluentBitwarden.Modules.AppState.Models;

public enum VaultTimeoutTrigger : byte
{
    AppIdle = 0,
    SystemIdle = 1
}


public enum VaultTimeout : int
{
    Never = -1,
    OneMinute = 60,
    FiveMinutes = 300,
    FifteenMinutes = 900,
    ThirtyMinutes = 1800,
}


public enum ClipboardClearDelay : uint
{
    Never = 0,
    Seconds10 = 10,
    Seconds30 = 30,
    Seconds60 = 60,
    Minutes2 = 120,
    Minutes5 = 300
}

public enum AppStartupBehavior : byte
{
    DoNothing = 0,
    OpenMainWindow = 1,
    StartMinimizedToTray = 2
}

public enum SensitiveActionPolicy : byte
{
    AllowWhenUnlocked = 0,
    RequireUserAction = 1,
}