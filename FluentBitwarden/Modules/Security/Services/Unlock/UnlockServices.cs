using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;

namespace FluentBitwarden.Modules.Security.Services.Unlock;

[Fody.ConfigureAwait(false)]
internal sealed class UnlockServices(
    IAccountRepository accountRepository,
    IAccountSecurityRepository accountSecurityRepository,
    ISessionTokensStore sessionTokensStore,
    CurrentSessionAccessor currentSessionAccessor,
    MasterPasswordUnlockStrategy masterPasswordUnlockStrategy) : IUnlockService
{
    public async Task<UnlockCapabilities> GetCapabilitiesAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var security = await accountSecurityRepository.GetByAccountIdAsync(userId, cancellationToken);
        if (security is null)
            return new UnlockCapabilities(false, false, 5);

        return new UnlockCapabilities(security.HasPin, security.HasWindowsHello, 5);
    }

    public async Task<UnlockResult> UnlockAsync<TRequest>(UserId userId, TRequest request, CancellationToken cancellationToken = default) where TRequest : struct, IUnlockRequest
    {
        if (await accountRepository.GetByIdAsync(userId, cancellationToken) is not { } storedAccount)
            return new UnlockResult.Failure("Account not found");

        ValueTask<UnlockResult> task = request switch
        {
            MasterPasswordUnlockRequest masterPasswordRequest => masterPasswordUnlockStrategy.UnlockAsync(storedAccount,
                masterPasswordRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request, null)
        };

        var result = await task;
        if (result is not UnlockResult.Success successUnlockResult)
            return result;

        var session = sessionTokensStore.Get(userId);
        if (session is null)
            return new UnlockResult.RequiresOnlineReauth();

        currentSessionAccessor.SetCurrentSession(storedAccount, session, successUnlockResult.Session);
        return result;
    }
}
