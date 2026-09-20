using FluentBitwarden.Contracts.Infrastructure.WindowsHello.Models;

namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello;

public interface IWindowsHelloIntegrationClient
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
