using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.Contracts.Modules.Accounts;

public interface IAccountWindowsHelloIntegrationClient
{
    Task<WindowsHelloEnrollmentStatus> GetEnrollmentAsync(
        GetWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    Task<WindowsHelloEnrollmentOutcome> EnableAsync(
        EnableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    Task DisableAsync(
        DisableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);
}
