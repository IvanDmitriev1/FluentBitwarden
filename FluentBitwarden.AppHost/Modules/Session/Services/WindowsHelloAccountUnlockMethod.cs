using BitwardenApi.Models;
using Dapper;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Infrastructure.Security.WindowsHello;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services;

using AccountUnlockOperationResult = OperationResult<AccountUnlockOutcome, DecryptedUserKey>;

internal sealed class WindowsHelloAccountUnlockMethod(ISqliteConnectionFactory connectionFactory)
{
    public UnlockMethodType UnlockMethod => UnlockMethodType.WindowsHello;

    public Task<bool> IsSupportedAsync() => WindowsHelloTpmKeyProtector.IsSupportedAsync();

    /// <summary>
    /// Stores the currently decrypted Bitwarden user key wrapped by a Windows Hello Passport key.
    /// </summary>
    public void Enable(AccountSession accountSession, IntPtr hwnd)
    {
        var keyName = accountSession.Profile.UserId.ToString();

        WindowsHelloTpmKeyProtector.CreateOrReplaceWrappingKey(keyName, hwnd);

        byte[] protectedBytes = WindowsHelloTpmKeyProtector.WrapUserKey(
            keyName,
            accountSession.DecryptedUserKey.Key,
            hwnd);

        using var connection = connectionFactory.OpenConnection();
        connection.Execute(
            """
            INSERT INTO account_tpm_cng_unlock_keys (user_id,protected_user_key)
            VALUES (@UserId, @ProtectedUserKey)
            ON CONFLICT(user_id) DO UPDATE SET
                protected_user_key = excluded.protected_user_key;
            """,
            new
            {
                UserId = accountSession.Profile.UserId.ToString(),
                ProtectedUserKey = protectedBytes
            });
    }

    public bool IsEnabled(UserId userId)
    {
        using var connection = connectionFactory.OpenConnection();

        return connection.ExecuteScalar<bool>("""
                                              SELECT EXISTS(
                                                  SELECT 1
                                                  FROM account_tpm_cng_unlock_keys
                                                  WHERE user_id = @UserId COLLATE NOCASE
                                              );
                                              """,
            new
            {
                UserId = userId.ToString()
            });
    }

    public void Disable(UserId userId) => RemoveWindowsHelloUnlock(userId.ToString());

    /// <summary>
    /// Restores the Bitwarden user key with Windows Hello and returns the account unlock result.
    /// </summary>
    public AccountUnlockOperationResult Unlock(AccountKeyMaterial accountKeyMaterial, IntPtr hwnd)
    {
        var keyName = accountKeyMaterial.UserId.ToString();

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
            {
                AccountUnlockOperationResult.WithoutPayload(new AccountUnlockOutcome.Failure(
                    "Windows Hello unlock is not enabled for this account. Unlock with your master password and enable Windows Hello again."));
            }

            byte[] decryptedBytes = WindowsHelloTpmKeyProtector.UnwrapUserKey(
                keyName,
                protectedBytes,
                hwnd);

            return AccountUnlockOperationResult.WithPayload(new AccountUnlockOutcome.Success(),
                new DecryptedUserKey(accountKeyMaterial.UserId, decryptedBytes));
        }
        catch (WindowsHelloAuthenticationCanceledException)
        {
            return AccountUnlockOperationResult.WithoutPayload(new AccountUnlockOutcome.WindowsHelloCancelled());
        }
        catch (WindowsHelloKeyUnavailableException)
        {
            RemoveWindowsHelloUnlock(keyName);

            return AccountUnlockOperationResult.WithoutPayload(new AccountUnlockOutcome.Failure(
                "Windows Hello unlock is no longer available for this account. Unlock with your master password and enable Windows Hello again."));
        }
        catch (CryptographicException e)
        {
            RemoveWindowsHelloUnlock(keyName);
            return AccountUnlockOperationResult.WithoutPayload(new AccountUnlockOutcome.Failure(e.Message));
        }
    }

    /// <summary>
    /// Removes the unusable Windows Hello Passport key and its saved encrypted user key.
    /// </summary>
    private void RemoveWindowsHelloUnlock(string keyName)
    {
        WindowsHelloTpmKeyProtector.TryDeleteWrappingKey(keyName);

        using var connection = connectionFactory.OpenConnection();
        connection.Execute(
            """
            DELETE FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = keyName
            });
    }
}
