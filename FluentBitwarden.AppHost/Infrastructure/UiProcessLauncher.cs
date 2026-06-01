namespace FluentBitwarden.AppHost.Infrastructure;

internal static class UiProcessLauncher
{
    private const string UiExecutableName = "FluentBitwarden.Ui.exe";
    private const string UiProjectDirectoryName = "FluentBitwarden.Ui";

    private static readonly string PackageRoot = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
    private static readonly string ExecutablePath = Path.Combine(PackageRoot, UiProjectDirectoryName, UiExecutableName);

    public static void ActivateMainWindow()
        => StartProcess(string.Empty);

    public static void ActivateOverlay()
        => StartProcess("--overlay");


    public static void Activate()
    {
        if (IsRunning())
        {
            ActivateMainWindow();
        }
        else
        {
            ActivateOverlay();
        }
    }

    public static void Exit()
    {
        if (IsRunning())
        {
            StartProcess("--exit");
        }
    }

    public static bool IsRunning()
    {
        var processes = Process.GetProcessesByName("FluentBitwarden.Ui");
        return processes.Length > 0;
    }

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
