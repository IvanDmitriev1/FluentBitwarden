using Microsoft.Windows.AppLifecycle;
using WinUIEx;

namespace FluentBitwarden.Ui.Application.Abstractions;

public interface IApplicationController
{
    WindowEx ActiveWindow { get; }

    void HandleActivation(AppActivationArguments arguments);
}
