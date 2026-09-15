namespace FluentBitwarden.ViewModels.Vault.Models;

public static class VaultLoginCipherExtensions
{
    public static string CreatedAtFormatted(this Fido2Credential? credential)
    {
        if (credential is null)
            return string.Empty;

        return $"Created {credential.CreationDate:g}";
    }
}