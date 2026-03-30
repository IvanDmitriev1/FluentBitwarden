using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models.Unlock;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IUnlockStrategy<in TRequest>
    where TRequest : struct, IUnlockRequest
{
    UnlockMethod Method { get; }

    ValueTask<UnlockResult> UnlockAsync(
        StoredAccount account,
        TRequest request,
        CancellationToken cancellationToken = default);

}