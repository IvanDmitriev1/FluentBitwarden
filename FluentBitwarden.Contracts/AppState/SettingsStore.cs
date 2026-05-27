using FluentBitwarden.Contracts.AppState.Abstractions;
using FluentBitwarden.Contracts.AppState.Services;

namespace FluentBitwarden.Contracts.AppState;

public static class SettingsStore
{
    public static ISettingsStore Instance { get; } = new ApplicationDataSettingsStore();
}