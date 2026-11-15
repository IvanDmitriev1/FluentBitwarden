using BitwardenApi.Primitives.Ids;
using BitwardenApi.Vault.Ciphers.Models;

namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

internal static class TestCiphers
{
    public static Cipher CreateLogin(string? id = null, string? name = null)
    {
        Cipher cipher = Cipher.CreateBlankCipher(CipherType.Login);
        if (id is not null)
            cipher.Id = new CipherId(id);
        if (name is not null)
            cipher.Name = name;
        return cipher;
    }
}
