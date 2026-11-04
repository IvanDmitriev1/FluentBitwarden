using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public partial record AppSessionState
{
    public sealed record NotAuthenticated : AppSessionState;

    public sealed record Locked(AccountProfile Account) : AppSessionState;

    public sealed record Unlocked : AppSessionState
    {
        public Unlocked(
            AccountProfile account,
            IUnlockedVault vault,
            IAccountKeySession accountKeySession,
            IServerNotificationsDispatcher serverNotificationsDispatcher)
        {
            if (account.UserId != accountKeySession.UserId)
                throw new InvalidOperationException("The unlocked vault and account key must match the selected account.");

            Account = account;
            Vault = vault;
            AccountKeySession = accountKeySession;
            ServerNotificationsDispatcher = serverNotificationsDispatcher;
        }

        public AccountProfile Account { get; }
        public IUnlockedVault Vault { get; }
        public IAccountKeySession AccountKeySession { get; }
        public IServerNotificationsDispatcher ServerNotificationsDispatcher { get; }

        public void Deconstruct(
            out AccountProfile account,
            out IUnlockedVault vault,
            out IAccountKeySession accountKeySession,
            out IServerNotificationsDispatcher serverNotificationsDispatcher) =>
            (account, vault, accountKeySession, serverNotificationsDispatcher) =
            (Account, Vault, AccountKeySession, ServerNotificationsDispatcher);

        protected override async ValueTask DisposeCoreAsync()
        {
            try
            {
                await ServerNotificationsDispatcher.DisposeAsync();
            }
            finally
            {
                try
                {
                    Vault.Dispose();
                }
                finally
                {
                    AccountKeySession.Dispose();
                }
            }
        }
    }
}
