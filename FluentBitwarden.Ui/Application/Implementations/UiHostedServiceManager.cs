using FluentBitwarden.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Application.Implementations;

internal sealed class UiHostedServiceManager : IUiHostedServiceManager, IDisposable
{
    private readonly IHostedService[] _processServices;

    private readonly SemaphoreSlim _processServicesStartLock = new(1, 1);
    private bool _processServiceStarted;

    public UiHostedServiceManager(IEnumerable<IHostedService> hostedServices)
    {
        _processServices = hostedServices.ToArray();
    }

    public async Task EnsureProcessServicesStarted()
    {
        await _processServicesStartLock.WaitAsync();
        try
        {
            if (_processServiceStarted)
                return;

            await Task.WhenAll(_processServices.Select(service => service.StartAsync(CancellationToken.None)));
            _processServiceStarted = true;
        }
        finally
        {
            _processServicesStartLock.Release();
        }
    }

    public void Dispose()
    {
        _processServicesStartLock.Dispose();
    }
}
