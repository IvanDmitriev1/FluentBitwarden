using System.Security.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration;
using FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration.Internal;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class WindowsHelloNcryptStatusTests
{
    [Fact]
    public void ThrowIfFailed_accepts_success_and_the_configured_ignored_status()
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(0, "operation");
        WindowsHelloNcryptStatus.ThrowIfFailed(WindowsHelloNcryptStatus.NteBadData, "operation", WindowsHelloNcryptStatus.NteBadData);
    }

    [Theory]
    [InlineData(unchecked((int)0x80090036))]
    [InlineData(unchecked((int)0x800704C7))]
    public void ThrowIfFailed_maps_cancellation_statuses(int status)
    {
        Assert.Throws<WindowsHelloAuthenticationCanceledException>(() =>
            WindowsHelloNcryptStatus.ThrowIfFailed(status, "unlock"));
    }

    [Theory]
    [InlineData(unchecked((int)0x8009000D))]
    [InlineData(unchecked((int)0x80090016))]
    public void ThrowIfFailed_maps_missing_key_statuses(int status)
    {
        Assert.Throws<WindowsHelloKeyUnavailableException>(() =>
            WindowsHelloNcryptStatus.ThrowIfFailed(status, "unlock"));
    }

    [Fact]
    public void ThrowIfFailed_preserves_operation_and_hex_status_for_other_errors()
    {
        CryptographicException exception = Assert.Throws<CryptographicException>(() =>
            WindowsHelloNcryptStatus.ThrowIfFailed(WindowsHelloNcryptStatus.NteBadData, "read protected key"));

        Assert.Contains("read protected key", exception.Message, StringComparison.Ordinal);
        Assert.Contains("0x80090005", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(unchecked((int)0x80090005), false)]
    public void Succeeded_reports_only_the_native_success_code(int status, bool expected)
    {
        Assert.Equal(expected, WindowsHelloNcryptStatus.Succeeded(status));
    }
}
