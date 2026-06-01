using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

internal interface IAccountLoginService
{
    ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken);
}