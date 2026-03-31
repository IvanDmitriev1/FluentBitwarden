using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Unlock;

namespace FluentBitwarden.Modules.Session.Services.Unlock;

[Fody.ConfigureAwait(false)]
internal sealed class UnlockServices(
    IAccountRepository accountRepository,
    ISessionTokensStore sessionTokensStore,
    CurrentSessionAccessor currentSessionAccessor,
    MasterPasswordUnlockStrategy masterPasswordUnlockStrategy) : IUnlockService
{
    public async Task<UnlockCapabilities> GetCapabilitiesAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAsync(userId, cancellationToken);
        if (account is null)
            return new UnlockCapabilities(false, false, 0);

        return new UnlockCapabilities(account.HasPin, account.HasWindowsHello, 5);
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