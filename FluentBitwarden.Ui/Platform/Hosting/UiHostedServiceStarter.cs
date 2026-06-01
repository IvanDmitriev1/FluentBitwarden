using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Hosting;

internal sealed class UiHostedServiceStarter(IEnumerable<IHostedService> hostedServices)
    : IUiHostedServiceStarter
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsStarted { get; private set; }

    public async Task EnsureStartedAsync()
    {
        if (IsStarted)
            return;

        await _gate.WaitAsync();

        try
        {
            if (IsStarted)
                return;

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(CancellationToken.None);
            }

            IsStarted = true;
        }
        catch (Exception exception)
        {
            UnhandledExceptionLogger.WriteException(exception);
        }
        finally
        {
            _gate.Release();
        }
    }
}
