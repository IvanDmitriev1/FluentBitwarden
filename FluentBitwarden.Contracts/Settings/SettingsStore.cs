namespace FluentBitwarden.Contracts.Settings;

public static class SettingsStore
{
    public static ISettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}