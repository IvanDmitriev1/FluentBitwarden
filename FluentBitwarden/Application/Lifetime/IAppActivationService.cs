using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Application.Lifetime;

public interface IAppActivationService
{
    Task InitializeAsync(AppActivationArguments initialActivation, CancellationToken cancellationToken = default);

    Task HandleAsync(AppActivationArguments activation, CancellationToken cancellationToken = default);
}
