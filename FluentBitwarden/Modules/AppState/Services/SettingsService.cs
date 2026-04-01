using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.AppState.Internal;
using FluentBitwarden.Modules.AppState.Models;
using System.Text.Json;
using Windows.Storage;

namespace FluentBitwarden.Modules.AppState.Services;

internal sealed class SettingsService : ISettingsService
{
    private static readonly string FilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");

    private readonly Lock _lock = new();
    private AppSettings? _currentSettings;

    public AppSettings Get()
    {
        if (_currentSettings is not null)
            return _currentSettings;

        using var _ = _lock.EnterScope();

        if (!File.Exists(FilePath))
        {
            ResetToDefaults();
            return _currentSettings;
        }

        try
        {
            using var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 256);
            var settings = JsonSerializer.Deserialize<AppSettings>(fileStream, AppStateJsonContext.Default.AppSettings) ?? AppSettings.CreateDefault();

            _currentSettings = settings;
            return settings;
        }
        catch (JsonException)
        {
            ResetToDefaults();

            return _currentSettings;
        }
    }

    [MemberNotNull(nameof(_currentSettings))]
    public void Save(AppSettings settings)
    {
        using var _ = _lock.EnterScope();
        using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None, 256);
        JsonSerializer.Serialize(fileStream, settings, AppStateJsonContext.Default.AppSettings);

        _currentSettings = settings;
    }

    [MemberNotNull(nameof(_currentSettings))]
    public void ResetToDefaults()
    {
        var settings = AppSettings.CreateDefault();
        Save(settings);
    }
}