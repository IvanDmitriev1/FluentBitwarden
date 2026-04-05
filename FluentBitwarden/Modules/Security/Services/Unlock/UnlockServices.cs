using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;

namespace FluentBitwarden.Modules.Security.Services.Unlock;

[Fody.ConfigureAwait(false)]
internal sealed class UnlockServices(
    IUnitOfWorkFactory unitOfWorkFactory,
    ISessionTokensStore sessionTokensStore,
    CurrentSessionAccessor currentSessionAccessor,
    MasterPasswordUnlockStrategy masterPasswordUnlockStrategy) : IUnlockService
{
    public Task<UnlockCapabilities> GetCapabilitiesAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var capabilities = new UnlockCapabilities(false, false, 5);
        return Task.FromResult(capabilities);
    }

    public async Task<UnlockResult> UnlockAsync<TRequest>(UserId userId, TRequest request, CancellationToken cancellationToken = default) where TRequest : struct, IUnlockRequest
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var accountDecryption = await Task.Run(() => unitOfWork.AccountDecryptionRepository.GetById(userId), cancellationToken);

        if (accountDecryption is null)
            return new UnlockResult.Failure("Account not found");

        ValueTask<UnlockResult> task = request switch
        {
            MasterPasswordUnlockRequest masterPasswordRequest => masterPasswordUnlockStrategy.UnlockAsync(
                accountDecryption,
                masterPasswordRequest,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request, null)
        };

        var result = await task;
        if (result is not UnlockResult.Success successUnlockResult)
            return result;

        var session = sessionTokensStore.Get(userId);
        if (session is null)
            return new UnlockResult.RequiresOnlineReauth();

        var storedAccount = await Task.Run(() => unitOfWork.AccountRepository.GetById(userId), cancellationToken);
        ArgumentNullException.ThrowIfNull(storedAccount);

        currentSessionAccessor.SetCurrentSession(storedAccount, session, successUnlockResult.userKey);
        return result;
    }
}
