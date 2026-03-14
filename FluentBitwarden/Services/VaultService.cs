using System.Net.Http;
using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Extensions;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal sealed class VaultService(
    IVaultCache vaultCache,
    ISessionManager sessionManager,
    IMasterPasswordUnlockService masterPasswordUnlockService,
    IPinUnlockService pinUnlockService,
    IVaultSyncService vaultSyncService,
    ICipherPayloadDecryptor cipherPayloadDecryptor,
    IVaultSyncWriter vaultSyncWriter)
    : IVaultService
{
    private const string NoStoredSessionMessage = "No persisted Bitwarden session is available.";
    private const string LockedVaultMessage = "The vault is locked. Unlock it to view cached items.";
    private const string NoCachedDataMessage = "No cached vault data is available yet.";
    private const string EmptySecretMessage = "Enter your PIN or master password.";

    public async ValueTask<VaultSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        return session switch
        {
            null => new VaultSessionState.NoSession(),
            { IsLocked: true } => new VaultSessionState.Locked(session),
            _ => new VaultSessionState.Unlocked(session),
        };
    }

    public ValueTask AdoptAuthenticationAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default)
        => sessionManager.CompleteAuthenticationAsync(authentication, cancellationToken);

    public ValueTask<VaultUnlockOutcome> UnlockAsync(
        string secret,
        CancellationToken cancellationToken = default)
        => UnlockCoreAsync(secret, cancellationToken);

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
        => sessionManager.LockAsync(cancellationToken);

    public async ValueTask LogoutAsync(CancellationToken cancellationToken = default)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);

        await sessionManager.LogoutAsync(cancellationToken).ConfigureAwait(false);

        if (session is not null)
        {
            await vaultCache.ClearAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<VaultSyncOutcome> SyncAsync(CancellationToken cancellationToken = default)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new VaultSyncOutcome.Unavailable(NoStoredSessionMessage);
        }

        try
        {
            VaultSyncStateRecord? syncState = await vaultCache
                .GetSyncStateAsync(session.AccountId, cancellationToken)
                .ConfigureAwait(false);

            string accessToken = await sessionManager.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            VaultSyncResult syncResult = await vaultSyncService.SyncAsync(
                new VaultSyncRequest(
                    session.Environment,
                    accessToken,
                    session.AccountId,
                    session.Email,
                    syncState is not null,
                    syncState?.RevisionDate,
                    syncState?.LastSyncUtc,
                    syncState?.CipherCount ?? 0,
                    syncState?.FolderCount ?? 0,
                    syncState?.CollectionCount ?? 0),
                vaultSyncWriter,
                cancellationToken).ConfigureAwait(false);

            return syncResult switch
            {
                VaultSyncResult.Updated updated => new VaultSyncOutcome.Success(updated.Summary),
                VaultSyncResult.NotModified notModified => new VaultSyncOutcome.Success(notModified.Summary),
                _ => throw new InvalidOperationException("Unsupported vault sync result."),
            };
        }
        catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new VaultSyncOutcome.Offline(ex.ToOfflineVaultMessage());
        }
    }

    public async ValueTask<VaultReadOutcome<IReadOnlyList<DecryptedCipher>>> ListCiphersAsync(
        CancellationToken cancellationToken = default)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Unavailable(NoStoredSessionMessage);
        }

        byte[]? userKey = sessionManager.GetUnlockedUserKeyCopy();
        if (userKey is null)
        {
            return new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Locked(LockedVaultMessage);
        }

        VaultSyncStateRecord? syncState = await vaultCache
            .GetSyncStateAsync(session.AccountId, cancellationToken)
            .ConfigureAwait(false);

        if (syncState is null)
        {
            CryptographicOperations.ZeroMemory(userKey);
            return new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.NoCachedData(NoCachedDataMessage);
        }

        try
        {
            List<DecryptedCipher> decrypted = [];
            string? decryptionFailure = null;

            await vaultCache.VisitCiphersAsync(
                session.AccountId,
                (item, payload, ct) =>
                {
                    VaultDecryptionOutcome<DecryptedCipher> outcome =
                        cipherPayloadDecryptor.DecryptCipher(item, payload, userKey);

                    return outcome switch
                    {
                        VaultDecryptionOutcome<DecryptedCipher>.Success success => AddAndContinue(success.Value),
                        VaultDecryptionOutcome<DecryptedCipher>.Failed failed => StopWithFailure(failed.Message),
                        _ => throw new InvalidOperationException("Unsupported vault decryption outcome."),
                    };
                },
                cancellationToken).ConfigureAwait(false);

            return decryptionFailure is null
                ? new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Success(decrypted)
                : new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.DecryptionFailed(decryptionFailure);

            ValueTask<bool> AddAndContinue(DecryptedCipher cipher)
            {
                decrypted.Add(cipher);
                return ValueTask.FromResult(true);
            }

            ValueTask<bool> StopWithFailure(string message)
            {
                decryptionFailure = message;
                return ValueTask.FromResult(false);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
        }
    }

    public async ValueTask<VaultReadOutcome<DecryptedCipher?>> GetCipherAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new VaultReadOutcome<DecryptedCipher?>.Unavailable(NoStoredSessionMessage);
        }

        byte[]? userKey = sessionManager.GetUnlockedUserKeyCopy();
        if (userKey is null)
        {
            return new VaultReadOutcome<DecryptedCipher?>.Locked(LockedVaultMessage);
        }

        VaultSyncStateRecord? syncState = await vaultCache
            .GetSyncStateAsync(session.AccountId, cancellationToken)
            .ConfigureAwait(false);

        if (syncState is null)
        {
            CryptographicOperations.ZeroMemory(userKey);
            return new VaultReadOutcome<DecryptedCipher?>.NoCachedData(NoCachedDataMessage);
        }

        try
        {
            DecryptedCipher? decryptedCipher = null;
            string? decryptionFailure = null;

            bool found = await vaultCache.VisitCipherAsync(
                session.AccountId,
                id,
                (item, payload, ct) =>
                {
                    VaultDecryptionOutcome<DecryptedCipher> outcome =
                        cipherPayloadDecryptor.DecryptCipher(item, payload, userKey);

                    switch (outcome)
                    {
                        case VaultDecryptionOutcome<DecryptedCipher>.Success success:
                            decryptedCipher = success.Value;
                            break;

                        case VaultDecryptionOutcome<DecryptedCipher>.Failed failed:
                            decryptionFailure = failed.Message;
                            break;

                        default:
                            throw new InvalidOperationException("Unsupported vault decryption outcome.");
                    }

                    return ValueTask.CompletedTask;
                },
                cancellationToken).ConfigureAwait(false);

            if (!found)
            {
                return new VaultReadOutcome<DecryptedCipher?>.Success(null);
            }

            return decryptionFailure is null
                ? new VaultReadOutcome<DecryptedCipher?>.Success(decryptedCipher)
                : new VaultReadOutcome<DecryptedCipher?>.DecryptionFailed(decryptionFailure);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
        }
    }

    private async ValueTask<VaultUnlockOutcome> UnlockCoreAsync(
        string secret,
        CancellationToken cancellationToken)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new VaultUnlockOutcome.Unavailable(NoStoredSessionMessage);
        }

        if (!session.IsLocked)
        {
            return new VaultUnlockOutcome.Success();
        }

        string normalizedSecret = secret?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedSecret))
        {
            return new VaultUnlockOutcome.InvalidCredentials(EmptySecretMessage);
        }

        bool isPinConfigured = await pinUnlockService.IsConfiguredAsync(session, cancellationToken).ConfigureAwait(false);
        if (isPinConfigured)
        {
            SessionUnlockOutcome pinOutcome = await pinUnlockService
                .UnlockAsync(session, normalizedSecret, cancellationToken)
                .ConfigureAwait(false);

            if (pinOutcome is not SessionUnlockOutcome.InvalidCredentials || !session.CanUnlockWithMasterPassword)
            {
                return pinOutcome.ToVaultUnlockOutcome();
            }
        }

        if (session.CanUnlockWithMasterPassword)
        {
            SessionUnlockOutcome masterPasswordOutcome = await masterPasswordUnlockService
                .UnlockAsync(session, normalizedSecret, cancellationToken)
                .ConfigureAwait(false);

            return masterPasswordOutcome.ToVaultUnlockOutcome();
        }

        return new VaultUnlockOutcome.Unavailable("This session cannot be unlocked with a secret.");
    }
}
