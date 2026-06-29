using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

public interface IUiProcessManager : IProcessManager
{
    void OpenItem(CipherId cipherId);
}
