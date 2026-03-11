using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class LocalUnlockStatusService(
    ILocalVaultStateStore stateStore,
    IWindowsHelloUnlockService windowsHelloUnlockService)
    : ILocalUnlockStatusService
{
    public async ValueTask<LocalUnlockStatus> GetAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultState? state = await stateStore
            .GetForAccountAsync(session.AccountId, cancellationToken)
            .ConfigureAwait(false);

        if (state is null)
        {
            return LocalUnlockStatus.Empty;
        }

        bool canUseWindowsHello = await windowsHelloUnlockService
            .CanSetupAsync(cancellationToken)
            .ConfigureAwait(false);

        return LocalUnlockStatusFactory.Create(state, canUseWindowsHello);
    }
}
