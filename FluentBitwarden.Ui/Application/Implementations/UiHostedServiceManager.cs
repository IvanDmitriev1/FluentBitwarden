using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Application.Implementations;

internal sealed class UiHostedServiceManager : IUiHostedServiceManager, IDisposable
{
    private readonly IHostedService _processService;
    private readonly IHostedService[] _vaultServices;

    private readonly SemaphoreSlim _vaultServiceSemaphoreSlim = new(1, 1);
    private readonly SemaphoreSlim _processServiceStartLock = new(1, 1);
    private bool _processServiceStarted;
    private bool _vaultServicesStarted;

    public UiHostedServiceManager(
        IIpcEventClient eventClient,
        IEnumerable<IHostedService> hostedServices)
    {
        _processService = eventClient as IHostedService
            ?? throw new InvalidOperationException(
                $"{nameof(IIpcEventClient)} must also implement {nameof(IHostedService)}.");

        _vaultServices = hostedServices
            .Where(service => !ReferenceEquals(service, _processService))
            .ToArray();

        _ = eventClient.Subscribe<VaultSessionStatusChangedEvent>(OnVaultSessionChanged);
    }
    private async Task OnVaultSessionChanged(VaultSessionStatusChangedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Status == VaultSessionStatus.Unlocked)
        {
            await EnsureVaultServicesStartedAsync(cancellationToken);
        }
        else
        {
            await EnsureVaultServicesStoppedAsync();
        }
    }

    public async Task EnsureProcessServicesStarted()
    {
        await _processServiceStartLock.WaitAsync();
        try
        {
            if (_processServiceStarted)
                return;

            await _processService.StartAsync(CancellationToken.None);
            _processServiceStarted = true;
        }
        finally
        {
            _processServiceStartLock.Release();
        }
    }

    public async Task EnsureVaultServicesStartedAsync(CancellationToken cancellationToken)
    {
        await _vaultServiceSemaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            if (_vaultServicesStarted)
                return;

            await Task.WhenAll(_vaultServices.Select(s => s.StartAsync(cancellationToken)));
            _vaultServicesStarted = true;
        }
        finally
        {
            _vaultServiceSemaphoreSlim.Release();
        }
    }

    public async Task EnsureVaultServicesStoppedAsync()
    {
        await _vaultServiceSemaphoreSlim.WaitAsync();
        try
        {
            if (!_vaultServicesStarted)
                return;

            await Task.WhenAll(_vaultServices.Reverse().Select(s => s.StopAsync(CancellationToken.None)));
            _vaultServicesStarted = false;
        }
        finally
        {
            _vaultServiceSemaphoreSlim.Release();
        }
    }

    public void Dispose()
    {
        _vaultServiceSemaphoreSlim.Dispose();
        _processServiceStartLock.Dispose();
    }
}
