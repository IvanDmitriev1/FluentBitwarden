using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.System.Threading;

namespace FluentBitwarden.Modules.Vault.Services;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("f01a75ee-ca34-4595-9640-94b61dfadd00")]
[ComSourceInterfaces(typeof(IBackgroundTask))]
public sealed class VaultNotificationBackgroundTask : IBackgroundTask
{
    private BackgroundTaskDeferral _deferral = null!;

    [MTAThread]
    public void Run(IBackgroundTaskInstance taskInstance)
    {
        
    }
}