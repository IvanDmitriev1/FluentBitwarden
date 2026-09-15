namespace FluentBitwarden.Application.Abstractions;

internal interface IUiHostedServiceManager
{
    Task EnsureProcessServicesStarted();
}
