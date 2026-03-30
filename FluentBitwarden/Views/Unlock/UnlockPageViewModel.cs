using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Unlock;

public sealed class UnlockPageViewModel : ObservableRecipient, IPageLifecycleAwareParam<IReadOnlyList<StoredAccount>>
{
    public Task OnLoadingAsync(IReadOnlyList<StoredAccount> param, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void OnUnloading()
    {
        
    }
}
