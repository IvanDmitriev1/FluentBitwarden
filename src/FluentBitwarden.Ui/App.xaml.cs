using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Ui;

public partial class App : IActivationHandler
{
    public App(IServiceProvider serviceProvider, IApplicationController applicationController)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _applicationController = applicationController;
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly IApplicationController _applicationController;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) =>
        Handle(AppInstance.GetCurrent().GetActivatedEventArgs());

    public void Handle(AppActivationArguments args)
    {
        _applicationController.HandleActivation(args);
    }
}
