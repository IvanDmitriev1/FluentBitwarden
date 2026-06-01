using FluentBitwarden.Contracts.Modules.AppState.Abstractions;
using FluentBitwarden.Contracts.Modules.AppState.Services;

namespace FluentBitwarden.Contracts.Modules.AppState;

public static class SettingsStore
{
    public static ISettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}