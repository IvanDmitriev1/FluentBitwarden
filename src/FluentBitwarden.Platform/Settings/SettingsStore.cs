using FluentBitwarden.Platform.Settings.Composition;

namespace FluentBitwarden.Platform.Settings;

public static class SettingsStore
{
    public static ICompositeSettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}
