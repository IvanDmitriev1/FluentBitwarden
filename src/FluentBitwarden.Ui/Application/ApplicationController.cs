using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Settings;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Infrastructure.Windowing;
using FluentBitwarden.Ui.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using WinUIEx;

namespace FluentBitwarden.Ui.Application;

internal sealed class ApplicationController(
    AppRootViewModel appRootViewModel,
    IAppSessionClient appSessionClient,
    IIpcExceptionNotifier exceptionNotifier,
    IIpcEventClient eventClient) : IHostedService, IApplicationController
{
    private WindowEx? _activeWindow;

    public WindowEx ActiveWindow =>
        _activeWindow ?? throw new InvalidOperationException("Application is not activated.");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventClient.Subscribe<AppSessionStateChangedEvent>((_, _) =>
            App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync));

        exceptionNotifier.UnlockedSessionRequired += exception =>
            App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync).SafeFireAndForget();

        return App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void HandleActivation(AppActivationArguments arguments)
    {
        if (arguments.Data is not ILaunchActivatedEventArgs launchArgs)
        {
            ReplaceWindow<MainWindow>();
            return;
        }

        var args = launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = UiActivationCommand.Parse(args);
        switch (command)
        {
            case UiActivationCommand.OpenCommand:
                ReplaceWindow<MainWindow>();
                break;
            case UiActivationCommand.OverlayCommand:
                ReplaceWindow<OverlayWindow>();
                break;
        }

        //TODO implement opening vault item
    }

    private async Task RefreshSessionAsync()
    {
        var session = await appSessionClient.GetStateAsync(new GetAppSessionStateRequest(), CancellationToken.None);
        appRootViewModel.ApplySession(session);
    }

    private void ReplaceWindow<TWindow>()
        where TWindow : WindowEx, new()
    {
        if (_activeWindow is not null)
        {
            _activeWindow.Closed -= OnWindowClosed;
            _activeWindow.Close();
        }

        _activeWindow = new TWindow();
        _activeWindow.Closed += OnWindowClosed;

        var currentTheme = SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey);
        var themeChangeable = _activeWindow as IThemeChangeable;
        themeChangeable?.ApplyTheme(currentTheme);

        _activeWindow.ShowAndActivate();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (!ReferenceEquals(_activeWindow, sender))
        {
            return;
        }

        _activeWindow.Closed -= OnWindowClosed;
        _activeWindow = null;
    }
}
