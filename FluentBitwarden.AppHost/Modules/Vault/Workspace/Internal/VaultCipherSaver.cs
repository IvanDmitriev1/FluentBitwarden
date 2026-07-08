using System.Security.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

/// <summary>
/// Encrypts a decrypted domain <see cref="VaultCipher"/> and pushes it to the server, creating it
/// when its id is empty and updating it otherwise. A fresh individual cipher key is generated on
/// every save and wrapped with the user key. The server's response is persisted to the local
/// cache and decrypted back into a domain <see cref="VaultCipher"/> — no follow-up sync needed.
/// </summary>
[Fody.ConfigureAwait(false)]
internal sealed class VaultCipherSaver(
    IVaultItemsApi vaultApiClient,
    VaultCipherRequestFactory requestFactory,
    IUnitOfWorkFactory unitOfWorkFactory)
{
    private const int CipherKeyByteLength = 64;

    public async Task<VaultCipher> SaveAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        VaultCipher cipher,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(userKey, cipher);

        var savedDto = cipher.Id.IsEmpty
            ? await vaultApiClient.CreateCipherAsync(accountContext, request, cancellationToken)
            : await vaultApiClient.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);

        Persist(userKey.UserId, in savedDto);

        // Personal-only scope: OrganizationId is always empty, so the user key itself is the
        // correct decrypt key (same rule IAccountKeyService.GetOrganizationKey applies for
        // empty org ids) — no organization key needed here.
        return VaultDataParser.ParseAndDecryptCipher(in savedDto, savedDto.Data, userKey);
    }

    private void Persist(UserId userId, ref readonly VaultCipherResponse dto)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        new VaultWriterRepository(unitOfWork.Transaction, userId).UpsertCipher(in dto);
        unitOfWork.SaveChanges();
    }

    private VaultCipherRequest BuildRequest(UserKey userKey, VaultCipher cipher)
    {
        Span<byte> cipherKey = stackalloc byte[CipherKeyByteLength];
        try
        {
            RandomNumberGenerator.Fill(cipherKey);

            var wrappedKey = EncString.Encrypt(cipherKey, userKey.Key);
            DateTime? lastKnownRevisionDate = cipher.Id.IsEmpty ? null : cipher.RevisionDate.UtcDateTime;

            return requestFactory.Build(cipher, cipherKey, wrappedKey, userKey.UserId.Value, lastKnownRevisionDate);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }
}
