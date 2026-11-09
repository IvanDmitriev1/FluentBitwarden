using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.Hosting;

internal sealed class WinUiHost(IHost host) : IWinUiHost
{
    public IServiceProvider Services => host.Services;

    [STAThread]
    public void Run()
    {
        host.Run();
    }

    public static WinUiHostApplicationBuilder<TApplication> CreateSingleInstanced<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication>(string key)
        where TApplication : Microsoft.UI.Xaml.Application, IActivationHandler
    {
        var builder = new WinUiHostApplicationBuilder<TApplication>();
        builder.UseSingleInstanceKey(key);

        builder.Services.AddSingleton<TApplication>();
        builder.Services.AddSingleton<IActivationHandler>(static sp => sp.GetRequiredService<TApplication>());

        return builder;
    }
}
