using System.Security.Cryptography;
using System.Text.Json;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

public sealed class LocalVaultUnlockStateRepository(IAppPaths paths)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    internal async ValueTask<LocalVaultUnlockerState?> GetForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        LocalVaultUnlockerState? state = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return null;
        }

        if (!string.Equals(state.AccountId, accountId, StringComparison.Ordinal) || state.Payload is null)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        return state;
    }

    internal async ValueTask<LocalVaultUnlockerState> RequireForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        LocalVaultUnlockerState? state = await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
        return state ?? throw new InvalidOperationException("No local vault unlocker is configured for this session.");
    }

    internal async ValueTask SaveAsync(LocalVaultUnlockerState state, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.UnlockStateFilePath)!);

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(state, SerializerOptions);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                await File.WriteAllBytesAsync(paths.UnlockStateFilePath, protectedBytes, cancellationToken).ConfigureAwait(false);
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
    }

    internal ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(paths.UnlockStateFilePath))
        {
            File.Delete(paths.UnlockStateFilePath);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> HasWindowsHelloEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.WindowsHello is not null;

    public async ValueTask<bool> HasPinEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.Pin is not null;

    private async ValueTask<LocalVaultUnlockerState?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(paths.UnlockStateFilePath))
        {
            return null;
        }

        byte[] protectedBytes = await File.ReadAllBytesAsync(paths.UnlockStateFilePath, cancellationToken).ConfigureAwait(false);

        try
        {
            byte[] jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                return JsonSerializer.Deserialize<LocalVaultUnlockerState>(jsonBytes, SerializerOptions);
            }
            catch (JsonException)
            {
                await ClearAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jsonBytes);
            }
        }
        catch (CryptographicException)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }
}
