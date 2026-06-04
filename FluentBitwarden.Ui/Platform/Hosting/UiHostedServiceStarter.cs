using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Hosting;

internal sealed class UiHostedServiceStarter(IEnumerable<IHostedService> hostedServices)
    : IUiHostedServiceStarter
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _isStarted;

    public async Task EnsureStartedAsync()
    {
        if (_isStarted)
            return;

        await _gate.WaitAsync();

        try
        {
            if (_isStarted)
                return;

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(CancellationToken.None);
            }

            _isStarted = true;
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
