using System.Security.Cryptography;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Services;

public sealed class DpapiSessionStore(IAppPaths paths) : ISessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public async ValueTask<SessionState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(paths.SessionFilePath))
        {
            return null;
        }

        try
        {
            byte[] protectedBytes = await File.ReadAllBytesAsync(paths.SessionFilePath, cancellationToken).ConfigureAwait(false);
            byte[] jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                return JsonSerializer.Deserialize<SessionState>(jsonBytes, SerializerOptions);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jsonBytes);
            }
        }
        catch (CryptographicException ex)
        {
            throw new NetworkUnavailableException("The persisted session could not be decrypted on this device.", ex);
        }
    }

    public async ValueTask SaveAsync(SessionState state, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.SessionFilePath)!);
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(state, SerializerOptions);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(paths.SessionFilePath, protectedBytes, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(jsonBytes);
        }
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(paths.SessionFilePath))
        {
            File.Delete(paths.SessionFilePath);
        }

        return ValueTask.CompletedTask;
    }
}
