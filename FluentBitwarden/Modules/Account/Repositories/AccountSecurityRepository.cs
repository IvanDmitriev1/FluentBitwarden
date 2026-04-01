using BitwardenApi.Modules.Identity.Models;
using Dapper;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using System.Linq;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountSecurityRepository(ISqliteConnectionFactory connectionFactory) : IAccountSecurityRepository
{
    private readonly struct AccountSecurityData
    {
        public UserId UserId { get; init; }
        public bool HasPin { get; init; }
        public bool HasWindowsHello { get; init; }

        public StoredAccountSecurity ToStoredAccountSecurity() => new(UserId, HasPin, HasWindowsHello);
    }

    public Task<StoredAccountSecurity?> GetByAccountIdAsync(UserId accountId, CancellationToken cancellationToken = default) =>
        connectionFactory.ExecuteAsync(connection =>
        {
            CommandDefinition command = new(
                """
                SELECT
                    user_id AS UserId,
                    has_pin AS HasPin,
                    has_windows_hello AS HasWindowsHello
                FROM account_security
                WHERE user_id = @UserId COLLATE NOCASE;
                """,
                new { UserId = accountId },
                cancellationToken: cancellationToken);

            AccountSecurityData[] rows = connection.Query<AccountSecurityData>(command).Take(2).ToArray();

            return rows.Length switch
            {
                0 => null,
                1 => rows[0].ToStoredAccountSecurity(),
                _ => throw new InvalidOperationException($"Expected a single account security row for user '{accountId}', but found {rows.Length}.")
            };
        }, cancellationToken);

    public Task UpdateAsync(StoredAccountSecurity security, CancellationToken cancellationToken = default) =>
        connectionFactory.ExecuteAsync(connection =>
        {
            CommandDefinition command = new(
                """
                INSERT INTO account_security (
                    user_id,
                    has_pin,
                    has_windows_hello
                )
                VALUES (
                    @UserId,
                    @HasPin,
                    @HasWindowsHello
                )
                ON CONFLICT(user_id) DO UPDATE SET
                    has_pin = excluded.has_pin,
                    has_windows_hello = excluded.has_windows_hello;
                """,
                security,
                cancellationToken: cancellationToken);

            connection.Execute(command);
        }, cancellationToken);
}
