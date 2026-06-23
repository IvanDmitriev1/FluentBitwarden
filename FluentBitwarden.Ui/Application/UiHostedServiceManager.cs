using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Application;

internal sealed class UiHostedServiceManager : IUiHostedServiceManager
{
    private readonly IHostedService _processService;
    private readonly IHostedService[] _vaultServices;

    private readonly SemaphoreSlim _vaultServiceSemaphoreSlim = new(1, 1);
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

    public void EnsureProcessServicesStarted()
    {
        lock (_processService)
        {
            if (_processServiceStarted)
                return;

            _ = _processService.StartAsync(CancellationToken.None);
            _processServiceStarted = true;
        }
    }

    public async Task EnsureVaultServicesStartedAsync(CancellationToken cancellationToken)
    {
        await _vaultServiceSemaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            if (_vaultServicesStarted)
                return;

            foreach (var service in _vaultServices)
            {
                await service.StartAsync(cancellationToken);
            }

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

            for (int index = _vaultServices.Length - 1; index >= 0; index--)
            {
                await _vaultServices[index].StopAsync(CancellationToken.None);
            }

            _vaultServicesStarted = false;
        }
        finally
        {
            _vaultServiceSemaphoreSlim.Release();
        }
    }
}
