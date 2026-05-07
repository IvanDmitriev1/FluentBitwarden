using System.Security.Cryptography;
using BitwardenApi.Modules.Identity.Models;
using Dapper;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Infrastructure.Security.Tmp;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Views.Shell;
using WinUIEx;

namespace FluentBitwarden.Modules.Session.Services;

public sealed class TpmCngAccountUnlockMethod(ISqliteConnectionFactory connectionFactory)
{
    public UnlockMethodType UnlockMethod => UnlockMethodType.WindowsHello;

    public void EnableTmpCngUnlock(AccountSession accountSession)
    {
        var keyName = accountSession.Profile.UserId.ToString();
        var ownerWindowHandle = MainWindow.Instance.GetWindowHandle();

        TpmCngDataProtector.CreateOrReplaceKey(keyName, new TpmCngUiOptions()
        {
            FriendlyName = "FluentBitwarden account unlock",
            Description = $"Unlock vault for {accountSession.Profile.Email}",
            UseContext = "Unlock your FluentBitwarden vault",
            CreationTitle = "Enable Windows Hello unlock",
        }, ownerWindowHandle);

        byte[] protectedBytes = TpmCngDataProtector.ProtectData(
            keyName,
            accountSession.DecryptedUserKey.Key,
            ownerWindowHandle);

        using var connection = connectionFactory.OpenConnection();
        connection.Execute(
            """
            INSERT INTO account_tpm_cng_unlock_keys (
                user_id,
                protected_user_key
            )
            VALUES (
                @UserId,
                @ProtectedUserKey
            )
            ON CONFLICT(user_id) DO UPDATE SET
                protected_user_key = excluded.protected_user_key;
            """,
            new
            {
                UserId = accountSession.Profile.UserId.ToString(),
                ProtectedUserKey = protectedBytes
            });
    }

    public AccountUnlockOutcome Unlock(AccountKeyMaterial accountKeyMaterial)
    {
        try
        {
            using var connection = connectionFactory.OpenConnection();
            byte[]? protectedBytes = connection.QuerySingleOrDefault<byte[]>(
                """
                SELECT protected_user_key
                FROM account_tpm_cng_unlock_keys
                WHERE user_id = @UserId COLLATE NOCASE;
                """,
                new
                {
                    UserId = accountKeyMaterial.UserId.ToString()
                });

            if (protectedBytes is null)
                return new AccountUnlockOutcome.WindowsHelloCancelled();

            var keyName = accountKeyMaterial.UserId.ToString();
            byte[] decryptedBytes = TpmCngDataProtector.UnprotectVaultKey(
                keyName,
                protectedBytes,
                MainWindow.Instance.GetWindowHandle());

            return new AccountUnlockOutcome.Success(new DecryptedUserKey(accountKeyMaterial.UserId, decryptedBytes));
        }
        catch (CryptographicException e)
        {
            return new AccountUnlockOutcome.Failure(e.Message);
        }
    }
}
