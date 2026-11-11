using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace FluentBitwarden.Ui;

public partial class App : IActivationHandler
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private readonly IServiceProvider _serviceProvider;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) =>
        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());

    public void HandleActivation(AppActivationArguments arguments)
    {
        var command = arguments.Data is ILaunchActivatedEventArgs launchArgs
            ? UiActivationCommand.Parse(launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            : new UiActivationCommand.OpenCommand();

        var session = GetRequiredService<AppSessionMonitor>().CurrentSession;
        GetRequiredService<IAppRouter>().Activate(command, session);
    }
}
