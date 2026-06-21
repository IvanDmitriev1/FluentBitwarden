using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace FluentBitwarden.CommandPalette;

public static class Program
{
    private const string ComServerArgument = "-RegisterProcessAsComServer";

    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || !StringComparer.Ordinal.Equals(args[0], ComServerArgument))
            return;

        using var extensionDisposed = new ManualResetEvent(false);
        var server = new ComServer();
        using var extension = new FluentBitwardenCommandPaletteExtension(extensionDisposed);
        try
        {
            server.RegisterClass<FluentBitwardenCommandPaletteExtension, IExtension>(() => extension);
            server.Start();
            extensionDisposed.WaitOne();
            server.Stop();
        }
        finally
        {
            server.UnsafeDispose();
        }
    }
}
