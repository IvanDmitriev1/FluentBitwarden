namespace FluentBitwarden.AppHost.Infrastructure;

internal static class UiProcessLauncher
{
    private const string UiExecutableName = "FluentBitwarden.Ui.exe";
    private const string UiProjectDirectoryName = "FluentBitwarden.Ui";

    private static readonly string PackageRoot = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
    private static readonly string ExecutablePath = Path.Combine(PackageRoot, UiProjectDirectoryName, UiExecutableName);

    public static void Activate()
        => StartProcess(string.Empty);

    public static void Exit()
        => StartProcess("--exit");

    private static void StartProcess(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ExecutablePath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(ExecutablePath),
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to activate FluentBitwarden UI: {ex}");
        }
    }
}
