using FluentBitwarden.Infrastracture.Hosting;

namespace FluentBitwarden;

public static class Program
{
    [STAThread]
    static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var builder = new WinUiHostApplicationBuilder<App>();
        builder.UseSingleInstance("FluentBitwardenUiSingleInstance");

        var host = builder.Build();
        host.Run();

        return 0;
    }
}
