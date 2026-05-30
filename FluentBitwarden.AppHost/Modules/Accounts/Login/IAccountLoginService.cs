using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

internal interface IAccountLoginService
{
    ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken);
}