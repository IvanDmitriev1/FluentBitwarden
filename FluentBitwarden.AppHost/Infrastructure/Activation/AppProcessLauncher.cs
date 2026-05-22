namespace FluentBitwarden.AppHost.Infrastructure.Activation;

internal static class AppProcessLauncher
{
    private const string UiExecutableName = "FluentBitwarden.Ui.exe";
    private const string UiProjectDirectoryName = "FluentBitwarden.Ui";

    public static void Activate(AppLifecycleCommand command)
    {
        try
        {
            string packageRoot = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string executablePath = Path.Combine(packageRoot, UiProjectDirectoryName, UiExecutableName);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to activate FluentBitwarden UI with '{command}': {ex}");
        }
    }
}
