using FluentBitwarden.Modules.AppState.Models;

namespace FluentBitwarden.Modules.AppState.Abstractions;

public interface ISettingsService
{
    AppSettings Get();
    void Save(AppSettings settings);
    void ResetToDefaults();
}