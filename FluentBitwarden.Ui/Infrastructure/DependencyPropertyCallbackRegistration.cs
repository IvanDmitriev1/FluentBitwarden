using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure;

internal sealed class DependencyPropertyCallbackRegistration(
    DependencyObject owner,
    DependencyProperty property,
    DependencyPropertyChangedCallback callback)
{
    private long _token;
    private bool _isRegistered;

    public void Register()
    {
        if (_isRegistered)
            return;

        _token = owner.RegisterPropertyChangedCallback(property, callback);
        _isRegistered = true;
    }

    public void Unregister()
    {
        if (!_isRegistered)
            return;

        owner.UnregisterPropertyChangedCallback(property, _token);
        _isRegistered = false;
    }
}
