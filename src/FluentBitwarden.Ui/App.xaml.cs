using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        Activate(UiActivationCommandParser.ParseInitial(
            Environment.GetCommandLineArgs()[1..], UiActivationCommandParser.GetLaunchArguments(activation)));
    }

    public void HandleActivation(AppActivationArguments arguments) =>
        Activate(UiActivationCommandParser.ParseLaunchArguments(UiActivationCommandParser.GetLaunchArguments(arguments)));

    private void Activate(UiActivationCommand command)
    {
        var session = GetRequiredService<IAppSession>().CurrentSession;
        GetRequiredService<IAppRouter>().Activate(command, session);
    }
}
