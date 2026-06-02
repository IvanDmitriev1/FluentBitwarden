namespace FluentBitwarden.Contracts.Infrastructure.Settings;

public static class SettingsStore
{
    public static ISettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}