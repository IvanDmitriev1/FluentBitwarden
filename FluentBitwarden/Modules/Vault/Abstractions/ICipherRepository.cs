using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface ICipherRepository
{
    List<Cipher> GetCiphers(DecryptedUserKey decryptedUserKey);
    Cipher GetCipher(CipherId cipherId, DecryptedUserKey decryptedUserKey);
}