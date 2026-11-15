namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

internal sealed class DesktopTestApp : App
{
    public DesktopTestApp(IServiceProvider services) : base(services) =>
        UnhandledException += (_, args) => Console.Error.WriteLine(args.Exception);

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // The fixture opens the window after starting the session monitor.
    }
}
