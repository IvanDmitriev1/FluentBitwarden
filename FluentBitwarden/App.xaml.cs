using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Application.Activation;
using FluentBitwarden.Infrastructure;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Views.Shell.Overlay;
using WinUI.DependencyInjection;
using WinUIEx;
using AppWindowManager = FluentBitwarden.Infrastructure.Implementations.WindowManager;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    private readonly AppActivationArguments _initialActivation;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    private readonly IServiceProvider _services = new ServiceCollection()
            .AddSingleton<AppWindowManager>()
            .AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<AppWindowManager>())
            .AddSingleton<IThemeService>(static sp => sp.GetRequiredService<AppWindowManager>())
            .AddSingleton<IUiHostedServiceStarter, UiHostedServiceStarter>()
            .AddSingleton<IMessenger>(StrongReferenceMessenger.Default)
            .AddViews()
            .AddInfrastructureServices()
            .BuildServiceProvider();

    public object GetRequiredService(Type type)
        => _services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();
        ValidationTrimDependencies.Preserve();

        UnhandledException += static (_, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            Debug.WriteLine(args.Exception.Message);
            UnhandledExceptionLogger.WriteException(args.Exception);
            args.SetObserved();
        };

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _initialActivation = initialActivation;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) =>
        HandleActivation(_initialActivation);

    public void HandleActivation(AppActivationArguments args)
    {
        var command = UiActivationCommandParser.From(args);
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => HandleActivation(command));
    }

    private void HandleActivation(UiActivationCommand command)
    {
        switch (command)
        {
            case UiActivationCommand.Exit:
                Exit();
                break;

            case UiActivationCommand.ShowOverlay:
                ShowWindow<OverlayWindow>();
                break;

            case UiActivationCommand.ShowMainWindow:
                ShowWindow<MainWindow>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    private void ShowWindow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TWindow>() where TWindow : WindowEx
    {
        var windowManager = GetRequiredService<IWindowManager>();
        var window = ActivatorUtilities.CreateInstance<TWindow>(_services);
        windowManager.SetWindow(window);
    }
}
