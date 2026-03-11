using System.Security.Cryptography;
using System.Text.Json;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Models.Settings;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.Storage;

internal sealed class AppSettingsStore(IAppPaths paths) : IAppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettingsDocument? _document;
    private bool _loaded;

    public async ValueTask<string> GetOrCreateDeviceIdentifierAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument document = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (Guid.TryParse(document.DeviceIdentifier, out Guid existing))
            {
                return existing.ToString("D");
            }

            string deviceIdentifier = Guid.NewGuid().ToString("D");
            AppSettingsDocument updated = CloneDocument(document) with
            {
                DeviceIdentifier = deviceIdentifier,
            };

            await SaveDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
            return deviceIdentifier;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<AppThemePreference> GetThemePreferenceAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return (await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false)).ThemePreference;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask SetThemePreferenceAsync(
        AppThemePreference themePreference,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(themePreference))
        {
            throw new ArgumentOutOfRangeException(nameof(themePreference));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument updated = CloneDocument(await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false)) with
            {
                ThemePreference = themePreference,
            };

            await SaveDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<LocalVaultState?> GetLocalVaultStateAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument document = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return document.Accounts.TryGetValue(accountId, out LocalVaultState? state)
                ? state
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask SaveLocalVaultStateAsync(
        LocalVaultState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(state.AccountId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument updated = CloneDocument(await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false));
            updated.Accounts[state.AccountId] = state;
            await SaveDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask ClearLocalVaultStateAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument updated = CloneDocument(await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false));
            if (!updated.Accounts.Remove(accountId))
            {
                return;
            }

            await SaveDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask ClearAllLocalVaultStatesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            AppSettingsDocument updated = CloneDocument(await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false));
            if (updated.Accounts.Count == 0)
            {
                return;
            }

            updated.Accounts.Clear();
            await SaveDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<AppSettingsDocument> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return _document ?? CreateDefaultDocument();
        }

        _document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
        _loaded = true;
        return _document;
    }

    private async ValueTask<AppSettingsDocument> LoadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(paths.ConfigFilePath))
        {
            return CreateDefaultDocument();
        }

        byte[] fileBytes = await File.ReadAllBytesAsync(paths.ConfigFilePath, cancellationToken).ConfigureAwait(false);

        try
        {
            try
            {
                byte[] jsonBytes = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);

                try
                {
                    AppSettingsDocument? document = JsonSerializer.Deserialize<AppSettingsDocument>(jsonBytes, SerializerOptions);
                    if (document is null)
                    {
                        DeleteInvalidConfigFile();
                        return CreateDefaultDocument();
                    }

                    return NormalizeDocument(document);
                }
                catch (JsonException)
                {
                    DeleteInvalidConfigFile();
                    return CreateDefaultDocument();
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(jsonBytes);
                }
            }
            catch (CryptographicException)
            {
                DeleteInvalidConfigFile();
                return CreateDefaultDocument();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileBytes);
        }
    }

    private async ValueTask SaveDocumentAsync(
        AppSettingsDocument document,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(document, SerializerOptions);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                await File.WriteAllBytesAsync(paths.ConfigFilePath, protectedBytes, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(jsonBytes);
        }

        _document = document;
    }

    private static AppSettingsDocument CreateDefaultDocument()
        => new()
        {
            ThemePreference = AppThemePreference.System,
            Accounts = new Dictionary<string, LocalVaultState>(StringComparer.Ordinal),
        };

    private static AppSettingsDocument CloneDocument(AppSettingsDocument document)
        => new()
        {
            DeviceIdentifier = document.DeviceIdentifier,
            ThemePreference = document.ThemePreference,
            Accounts = new Dictionary<string, LocalVaultState>(document.Accounts, StringComparer.Ordinal),
        };

    private static AppSettingsDocument NormalizeDocument(AppSettingsDocument document)
        => new()
        {
            DeviceIdentifier = Guid.TryParse(document.DeviceIdentifier, out Guid existing)
                ? existing.ToString("D")
                : null,
            ThemePreference = Enum.IsDefined(document.ThemePreference)
                ? document.ThemePreference
                : AppThemePreference.System,
            Accounts = document.Accounts is { } accounts
                ? new Dictionary<string, LocalVaultState>(accounts, StringComparer.Ordinal)
                : new Dictionary<string, LocalVaultState>(StringComparer.Ordinal),
        };

    private void DeleteInvalidConfigFile()
    {
        if (File.Exists(paths.ConfigFilePath))
        {
            File.Delete(paths.ConfigFilePath);
        }
    }
}
