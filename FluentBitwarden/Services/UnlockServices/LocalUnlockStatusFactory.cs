using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal static class LocalUnlockStatusFactory
{
    public static LocalUnlockStatus Create(
        LocalVaultState? state,
        bool canUseWindowsHello)
    {
        if (state is null)
        {
            return LocalUnlockStatus.Empty;
        }

        return new LocalUnlockStatus(
            true,
            state.WindowsHello is not null
                ? UnlockMethodStatus.Configured
                : canUseWindowsHello
                    ? UnlockMethodStatus.Available
                    : UnlockMethodStatus.Unavailable,
            state.Pin is not null
                ? UnlockMethodStatus.Configured
                : UnlockMethodStatus.Available);
    }
}
