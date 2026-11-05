using FluentBitwarden.Ui.Infrastracture.Hosting.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Infrastracture.Hosting;

internal sealed class WinUiHost(IHost host) : IWinUiHost
{
    public IServiceProvider Services => host.Services;

    [STAThread]
    public void Run()
    {
        host.Run();
    }
}
