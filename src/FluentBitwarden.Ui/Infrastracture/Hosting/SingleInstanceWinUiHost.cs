using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastracture.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace FluentBitwarden.Infrastracture.Hosting;

internal sealed class SingleInstanceWinUiHost<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]  TApplication>(IHost host, string instanceKey) : IWinUiHost
    where TApplication : Application
{
    private Microsoft.Win32.SafeHandles.SafeFileHandle? _redirectEventHandle;

    public IServiceProvider Services => host.Services;


    [STAThread]
    public void Run()
    {
        var keyInstance = AppInstance.FindOrRegisterForKey(instanceKey);
        var activationEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(activationEventArgs, keyInstance);
            return;
        }

        keyInstance.Activated += KeyInstanceOnActivated;

        host.Run();
    }

    private void KeyInstanceOnActivated(object? sender, AppActivationArguments e)
    {
        var handler = Services.GetRequiredService<IActivationHandler>();
        Services.GetRequiredService<WinUiHostedService<TApplication>>().RunOnUiThread(() => handler.Handle(e));
    }

    // Do the redirection on another thread, and use a non-blocking
    // wait method to wait for the redirection to complete.
    private void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        _redirectEventHandle = PInvoke.CreateEvent(null, bManualReset: true, bInitialState: false, lpName: null);
        PInvoke.AllowSetForegroundWindow(keyInstance.ProcessId);

        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            PInvoke.SetEvent(_redirectEventHandle);
        });

        const uint CoWaitDefault = 0;
        const uint Infinite = 0xFFFFFFFF;

        HANDLE rawHandle = new(_redirectEventHandle.DangerousGetHandle());
        PInvoke.CoWaitForMultipleObjects(CoWaitDefault, Infinite, [rawHandle], out _);
    }

}
