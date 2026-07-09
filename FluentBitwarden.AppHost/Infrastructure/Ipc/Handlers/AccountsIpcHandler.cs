using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

[Fody.ConfigureAwait(false)]
internal sealed class AccountsIpcHandler(
    IAccountStore accountStore,
    IAccountLoginService accountLoginService) : IAccountsClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Account.GetAccounts)]
    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = accountStore.GetAccounts();
        return ValueTask.FromResult(accounts);
    }

    public ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default) =>
        accountLoginService.LoginAsync(request, cancellationToken);
}
