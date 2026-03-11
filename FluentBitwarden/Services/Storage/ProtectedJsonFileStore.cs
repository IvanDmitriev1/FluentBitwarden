using System.Security.Cryptography;
using System.Text.Json;

namespace FluentBitwarden.Services.Storage;

internal sealed class ProtectedJsonFileStore<TState>(string filePath)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public async ValueTask<TState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        byte[] protectedBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

        try
        {
            byte[] jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                return JsonSerializer.Deserialize<TState>(jsonBytes, SerializerOptions);
            }
            catch (JsonException)
            {
                await ClearAsync(cancellationToken).ConfigureAwait(false);
                return default;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jsonBytes);
            }
        }
        catch (CryptographicException)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return default;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    public async ValueTask SaveAsync(TState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(state, SerializerOptions);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                await File.WriteAllBytesAsync(filePath, protectedBytes, cancellationToken).ConfigureAwait(false);
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

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return ValueTask.CompletedTask;
    }
}
