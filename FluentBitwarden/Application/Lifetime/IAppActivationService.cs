using Microsoft.UI.Xaml;

namespace FluentBitwarden.Application.Lifetime;

public interface IAppActivationService
{
    void Activate(LaunchActivatedEventArgs args);
    void ReopenMainWindow();
}