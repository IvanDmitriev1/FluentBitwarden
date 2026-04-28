using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Passkey.Internal;

internal static class WebAuthnAssertionData
{
    private const int RpIdHashLength = 32;

    private const byte FlagUserPresent = 0x01;      // UP, bit 0
    private const byte FlagUserVerified = 0x04;     // UV, bit 2
    private const byte FlagBackupEligible = 0x08;   // BE, bit 3
    private const byte FlagBackupState = 0x10;      // BS, bit 4
}