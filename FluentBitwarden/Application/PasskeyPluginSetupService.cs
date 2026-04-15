using Windows.ApplicationModel;

namespace FluentBitwarden.Application;

public static class PasskeyPluginSetupService
{
    private const int Windows11_24H2Build = 26100;

    public static Task EnsureRegisteredAsync()
        => RunPackagedComServerAsync("--register-plugin");

    public static Task UnregisterAsync()
        => RunPackagedComServerAsync("--unregister-plugin");

    private static async Task RunPackagedComServerAsync(string arguments)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, Windows11_24H2Build))
        {
            return;
        }

        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppWithArgumentsAsync(arguments);
    }
}
