using FluentBitwarden.Models;

namespace FluentBitwarden.ViewModels;

internal sealed record LoginUnlockCapabilities(
    bool WindowsHelloAvailable,
    bool PinAvailable,
    bool MasterPasswordAvailable)
{
    public LoginUnlockMethodItem[] BuildOptions(
        WindowsHelloUnlockViewModel windowsHelloUnlock,
        MasterPasswordUnlockViewModel masterPasswordUnlock,
        PinUnlockViewModel pinUnlock)
    {
        List<LoginUnlockMethodItem> methods = [];

        if (WindowsHelloAvailable)
        {
            methods.Add(windowsHelloUnlock.Method);
        }

        if (PinAvailable)
        {
            methods.Add(pinUnlock.Method);
        }

        if (MasterPasswordAvailable)
        {
            methods.Add(masterPasswordUnlock.Method);
        }

        return [.. methods];
    }

    public LoginUnlockMethod? DeterminePreferredMethod()
    {
        if (WindowsHelloAvailable)
        {
            return LoginUnlockMethod.WindowsHello;
        }

        if (PinAvailable)
        {
            return LoginUnlockMethod.Pin;
        }

        return MasterPasswordAvailable
            ? LoginUnlockMethod.MasterPassword
            : null;
    }
}
