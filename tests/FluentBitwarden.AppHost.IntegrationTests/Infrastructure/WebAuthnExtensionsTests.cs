using System.Runtime.InteropServices;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn.Internal;
using Windows.Win32.Foundation;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class WebAuthnExtensionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ThrowWebAuthnExceptionOnFailure_returns_for_successful_results(int result)
    {
        new HRESULT(result).ThrowWebAuthnExceptionOnFailure();
    }

    [Fact]
    public void ThrowWebAuthnExceptionOnFailure_wraps_an_ordinary_failure_and_preserves_hresult()
    {
        var result = new HRESULT(unchecked((int)0x80004005));

        WebAuthnLoginException exception = Assert.Throws<WebAuthnLoginException>(
            () => result.ThrowWebAuthnExceptionOnFailure());

        COMException innerException = Assert.IsType<COMException>(exception.InnerException);
        Assert.Equal(result.Value, innerException.HResult);
        Assert.Contains("Windows passkey authentication failed", exception.Message, StringComparison.Ordinal);
    }
}
