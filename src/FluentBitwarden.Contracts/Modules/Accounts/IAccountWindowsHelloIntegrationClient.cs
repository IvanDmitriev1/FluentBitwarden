using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.Contracts.Modules.Accounts;

public interface IAccountWindowsHelloIntegrationClient
{
    ValueTask<WindowsHelloEnrollmentStatus> GetEnrollmentAsync(
        GetWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<WindowsHelloEnrollmentOutcome> EnableAsync(
        EnableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    ValueTask DisableAsync(
        DisableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default);
}
