using BitwardenApi.Models;

namespace FluentBitwarden.Views.Accounts.SignIn.Presentation;

public static class BitwardenEnvironmentExtensions
{
    public static string ToServerDisplayName(this BitwardenEnvironment environment)
    {
        if (environment == BitwardenEnvironment.UnitedStates)
        {
            return "Bitwarden US";
        }

        if (environment == BitwardenEnvironment.Europe)
        {
            return "Bitwarden EU";
        }

        return "Custom";
    }
}