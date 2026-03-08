using System.Threading;
using System.Threading.Tasks;

namespace FluentBitwarden.Ui.Abstractions;

public interface IPageLifecycleAware
{
    Task OnLoadingAsync(CancellationToken cancellationToken);

    Task OnUnloadingAsync(CancellationToken cancellationToken);
}
