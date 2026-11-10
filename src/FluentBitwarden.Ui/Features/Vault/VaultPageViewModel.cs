using FluentBitwarden.Ui.Features.Vault.Intents;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Features.Vault;

public sealed partial class VaultPageViewModel : ObservableObject, INavigationAware, INavigationAware<VaultCipherNavigationIntent>
{
    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask OnNavigatedToAsync(VaultCipherNavigationIntent parameter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
