using BitwardenApi.Client;
using BitwardenApi.Vault.Ciphers.Requests;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultCipherService(
    IAppSessionAccessor appSessionAccessor,
    VaultWriterRepository vaultWriterRepository,
    IUnitOfWork unitOfWork,
    ICiphersApi ciphersApi) : IVaultCipherService
{
    public async Task<Cipher> SaveAsync(Cipher cipher, CancellationToken cancellationToken = default)
    {
        var (accountProfile, vault, keySession, _) = appSessionAccessor.RequireUnlockedState();
        BitwardenAccountContext accountContext = accountProfile.BitwardenAccountContext;
        CipherRequest request = CipherRequestFactory.BuildRequest(keySession.UserKey, cipher);
        CipherResponse savedDto = cipher.Id.IsEmpty
            ? await ciphersApi.CreateCipherAsync(accountContext, request, cancellationToken)
            : await ciphersApi.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);
        var savedCipher = VaultDataLoader.DecryptSavedCipher(in savedDto, keySession);

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(accountProfile.UserId, in savedDto);
        unitOfWork.Commit();
        vault.UpsertCipher(savedCipher);
        return savedCipher;
    }
}
