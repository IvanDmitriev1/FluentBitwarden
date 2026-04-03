using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Views.Unlock.Models;

public readonly record struct UnlockOption(UnlockMethod Method, string Title)
{
    public static IReadOnlyList<UnlockOption> CreateUnlockOptions(in UnlockCapabilities capabilities)
    {
        int size = 1 + Convert.ToInt32(capabilities.SupportsPin) + Convert.ToInt32(capabilities.SupportsWindowsHello);

        var methods = new List<UnlockOption>(size);
        methods.Add(new UnlockOption(UnlockMethod.MasterPassword, "Master password"));

        if (capabilities.SupportsPin)
        {
            methods.Add(new UnlockOption(UnlockMethod.Pin, "Pin"));
        }

        if (capabilities.SupportsWindowsHello)
        {
            methods.Add(new UnlockOption(UnlockMethod.WindowsHello, "Windows Hello"));
        }

        return methods;
    }
}