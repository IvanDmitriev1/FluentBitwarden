using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Services.Window;
using FluentBitwarden.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Views.Shell;
using WinUI.DependencyInjection;
using WinUIEx;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    private readonly AppActivationArguments _initialActivation;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    private readonly IServiceProvider _services;

    public object GetRequiredService(Type type)
        => _services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();
        TrimmingConfiguration.Preserve();

        UnhandledException += static (_, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            Debug.WriteLine(args.Exception.Message);
            UnhandledExceptionLogger.WriteException(args.Exception);
            args.SetObserved();
        };

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _initialActivation = initialActivation;

        var services = new ServiceCollection()
            .AddSingleton<IMessenger>(StrongReferenceMessenger.Default)
            .AddViews()
            .AddUiServices();
#if DEBUG
        _services = services.BuildServiceProvider(true);
#else
        _services = services.BuildServiceProvider();
#endif
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        HandleActivation(_initialActivation);
    }

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
        if (windowManager.HasWindow)
        {
            windowManager.ActiveWindow.ShowAndActivate();
            return;
        }

        var window = ActivatorUtilities.CreateInstance<TWindow>(_services);
        windowManager.SetWindow(window);
    }
}
