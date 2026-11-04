using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Infrastracture.Hosting.Abstractions;

public interface IActivationHandler
{
    void Handle(AppActivationArguments args);
}
