using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;

public interface IActivationHandler
{
    void Handle(AppActivationArguments args);
}
