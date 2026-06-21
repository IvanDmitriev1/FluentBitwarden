namespace FluentBitwarden.Platform.Settings;

public static class SettingsStore
{
    public static ICompositeSettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}