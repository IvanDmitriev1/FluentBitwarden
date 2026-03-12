using System.Net.Http;
using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal sealed class VaultService(
    IVaultCache vaultCache,
    ISessionManager sessionManager,
    IMasterPasswordUnlockService masterPasswordUnlockService,
    IPinUnlockService pinUnlockService,
    IVaultDataService vaultDataService)
    : IVaultService
{
    private const string NoStoredSessionMessage = "No persisted Bitwarden session is available.";
    private const string LockedVaultMessage = "The vault is locked. Unlock it to view cached items.";
    private const string NoCachedDataMessage = "No cached vault data is available yet.";
    private const string EmptySecretMessage = "Enter your PIN or master password.";

    public async ValueTask<VaultSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default)
    {
        StoredSessionInfo? session = await sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(false);
        return VaultSessionStateFactory.Create(session);
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
            VaultSyncResult syncResult = await vaultDataService.SyncAsync(
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
                cancellationToken).ConfigureAwait(false);

            return syncResult switch
            {
                VaultSyncResult.Updated updated => await SaveUpdatedSnapshotAsync(updated, cancellationToken).ConfigureAwait(false),
                VaultSyncResult.NotModified notModified => new VaultSyncOutcome.Success(notModified.Summary),
                _ => throw new InvalidOperationException("Unsupported vault sync result."),
            };
        }
        catch (NetworkUnavailableException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new VaultSyncOutcome.Offline(FormatOfflineMessage(ex));
        }
        catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new VaultSyncOutcome.Offline(FormatOfflineMessage(ex));
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
            IReadOnlyList<EncryptedCipherRecord> records = await vaultCache
                .ListCiphersAsync(session.AccountId, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<DecryptedCipher> ciphers = vaultDataService.DecryptCiphers(records, userKey);

            return new VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Success(ciphers);
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
            EncryptedCipherRecord? record = await vaultCache
                .GetCipherAsync(session.AccountId, id, cancellationToken)
                .ConfigureAwait(false);

            return new VaultReadOutcome<DecryptedCipher?>.Success(
                record is null ? null : vaultDataService.DecryptCipher(record, userKey));
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
                return MapUnlockOutcome(pinOutcome);
            }
        }

        if (session.CanUnlockWithMasterPassword)
        {
            SessionUnlockOutcome masterPasswordOutcome = await masterPasswordUnlockService
                .UnlockAsync(session, normalizedSecret, cancellationToken)
                .ConfigureAwait(false);

            return MapUnlockOutcome(masterPasswordOutcome);
        }

        return new VaultUnlockOutcome.Unavailable("This session cannot be unlocked with a secret.");
    }

    private async ValueTask<VaultSyncOutcome> SaveUpdatedSnapshotAsync(
        VaultSyncResult.Updated updated,
        CancellationToken cancellationToken)
    {
        await vaultCache.SaveSyncAsync(updated.Snapshot, cancellationToken).ConfigureAwait(false);
        return new VaultSyncOutcome.Success(updated.Summary);
    }

    private static VaultUnlockOutcome MapUnlockOutcome(SessionUnlockOutcome outcome)
        => outcome switch
        {
            SessionUnlockOutcome.Success => new VaultUnlockOutcome.Success(),
            SessionUnlockOutcome.InvalidCredentials invalidCredentials => new VaultUnlockOutcome.InvalidCredentials(invalidCredentials.Message),
            SessionUnlockOutcome.Unavailable unavailable => new VaultUnlockOutcome.Unavailable(unavailable.Message),
            SessionUnlockOutcome.Cancelled cancelled => new VaultUnlockOutcome.Cancelled(cancelled.Message),
            _ => throw new InvalidOperationException("Unsupported session unlock outcome."),
        };

    private static string FormatOfflineMessage(Exception exception)
        => string.IsNullOrWhiteSpace(exception.Message)
            ? "The vault could not reach Bitwarden. Cached data is still available offline."
            : exception.Message;
}
