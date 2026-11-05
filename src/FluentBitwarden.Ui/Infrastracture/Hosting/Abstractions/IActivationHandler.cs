using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Ui.Infrastracture.Hosting.Abstractions;

public interface IActivationHandler
{
    void Handle(AppActivationArguments args);
}
