using BitwardenApi.Client;
using BitwardenApi.Vault.Ciphers.Requests;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultCipherService(
    IAppSessionLease appSessionLease,
    VaultWriterRepository vaultWriterRepository,
    IUnitOfWork unitOfWork,
    ICiphersApi ciphersApi) : IVaultCipherService
{
    public async Task<Cipher> SaveAsync(Cipher cipher, CancellationToken cancellationToken = default)
    {
        var (accountProfile, vault, keySession, _) = appSessionLease.RequireUnlockedState();
        BitwardenAccountContext accountContext = accountProfile.BitwardenAccountContext;
        CipherResponse savedDto;
        if (cipher.Id.IsEmpty)
        {
            CipherRequest request = CipherRequestBuilder.Build(keySession.UserKey, cipher);
            savedDto = await ciphersApi.CreateCipherAsync(accountContext, request, cancellationToken);
        }
        else
        {
            CipherResponse original = await ciphersApi.GetCipherAsync(accountContext, cipher.Id, cancellationToken);
            if (cipher.RevisionDate != original.RevisionDate)
                throw new InvalidOperationException("The cipher changed since it was opened. Sync before saving.");
            CipherRequest request = CipherUpdateRequestBuilder.Build(keySession.UserKey, cipher, in original);
            savedDto = await ciphersApi.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);
        }

        var savedCipher = VaultDataLoader.DecryptSavedCipher(in savedDto, keySession);

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(accountProfile.UserId, in savedDto);
        unitOfWork.Commit();
        vault.UpsertCipher(savedCipher);
        return savedCipher;
    }
}
