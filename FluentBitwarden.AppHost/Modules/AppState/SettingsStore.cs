using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.AppState.Services;

namespace FluentBitwarden.Modules.AppState;

public static class SettingsStore
{
    public static ISettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}