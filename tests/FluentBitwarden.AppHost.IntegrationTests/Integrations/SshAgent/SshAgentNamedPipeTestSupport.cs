using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

internal static partial class SshAgentNamedPipeTestSupport
{
    private const uint NmpwaitNowait = 1;
    private static readonly TimeSpan ListenerTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(10);

    internal static async Task WaitForListenerAsync(string pipeName, CancellationToken cancellationToken)
    {
        string nativePipeName = $@"\\.\pipe\{pipeName}";
        var stopwatch = Stopwatch.StartNew();

        while (!WaitNamedPipeW(nativePipeName, NmpwaitNowait))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopwatch.Elapsed >= ListenerTimeout)
            {
                throw new TimeoutException($"Named pipe listener '{pipeName}' did not become ready.");
            }

            await Task.Delay(ProbeInterval, cancellationToken);
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "WaitNamedPipeW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WaitNamedPipeW(string lpNamedPipeName, uint nTimeout);
}
