using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace FluentBitwarden.Ui;

public partial class App : IActivationHandler
{
    public App(IServiceProvider serviceProvider, IWindowManager windowManager)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _windowManager = windowManager;
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowManager _windowManager;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) =>
        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());

    public void HandleActivation(AppActivationArguments arguments)
    {
        if (arguments.Data is not ILaunchActivatedEventArgs launchArgs)
        {
            _windowManager.ReplaceWindow<MainWindow>();
            return;
        }

        var args = launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = UiActivationCommand.Parse(args);
        switch (command)
        {
            case UiActivationCommand.OpenCommand:
                _windowManager.ReplaceWindow<MainWindow>();
                break;
            case UiActivationCommand.OverlayCommand:
                _windowManager.ReplaceWindow<OverlayWindow>();
                break;
        }

        //TODO implement opening vault item
    }
}
