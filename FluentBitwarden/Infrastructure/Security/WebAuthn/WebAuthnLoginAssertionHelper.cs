using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Views.Shell;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Authentication.WebAuthn;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Security.WebAuthn;

internal static unsafe class WebAuthnLoginAssertionHelper
{
    private const string PublicKeyCredentialType = "public-key";
    private const string Sha256HashAlgorithm = "SHA-256";

    public static WebAuthnLoginAssertionResponseRequest GetAssertion(WebAuthnLoginAssertionOptions options)
    {
        nint hwnd = MainWindow.Instance.GetWindowHandle();

        string origin = $"{Uri.UriSchemeHttps}://{options.RpId}";

        byte[] clientDataJson = BuildClientDataJson(
            challenge: options.Challenge,
            origin: origin);

        return GetAssertionCore(
            hwnd: hwnd,
            rpId: options.RpId,
            timeoutMilliseconds: options.TimeoutMilliseconds,
            clientDataJson: clientDataJson);
    }

    private static WebAuthnLoginAssertionResponseRequest GetAssertionCore(
        nint hwnd,
        string rpId,
        uint timeoutMilliseconds,
        byte[] clientDataJson)
    {
        using NativeAssertionHandle assertionHandle = new();

        fixed (byte* clientDataJsonPointer = clientDataJson)
        fixed (char* sha256Pointer = Sha256HashAlgorithm)
        {
            WEBAUTHN_CLIENT_DATA clientData = new()
            {
                dwVersion = 1,
                cbClientDataJSON = checked((uint)clientDataJson.Length),
                pbClientDataJSON = clientDataJsonPointer,
                pwszHashAlgId = new PCWSTR(sha256Pointer)
            };

            WEBAUTHN_AUTHENTICATOR_GET_ASSERTION_OPTIONS assertionOptions = new()
            {
                dwVersion = PInvoke.WebAuthNGetApiVersionNumber(),
                dwTimeoutMilliseconds = timeoutMilliseconds,
                CredentialList = default,
                dwUserVerificationRequirement = 1,
            };

            HRESULT result = PInvoke.WebAuthNAuthenticatorGetAssertion(
                new HWND(hwnd),
                rpId,
                clientData,
                assertionOptions,
                out WEBAUTHN_ASSERTION* assertion);

            assertionHandle.Set(assertion);
            result.ThrowWebAuthnExceptionOnFailure();
        }

        return CreateResponse(assertionHandle.Value, clientDataJson);
    }

    private static byte[] BuildClientDataJson(byte[] challenge, string origin)
    {
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "webauthn.get");
        writer.WriteString("challenge", Base64Url.EncodeToString(challenge));
        writer.WriteString("origin", origin);
        writer.WriteBoolean("crossOrigin", false);
        writer.WriteEndObject();

        writer.Flush();

        return stream.ToArray();
    }

    private static WebAuthnLoginAssertionResponseRequest CreateResponse(
        WEBAUTHN_ASSERTION* assertion,
        byte[] clientDataJson)
    {
        if (assertion is null)
        {
            throw new WebAuthnLoginException(
                "Windows did not return a passkey assertion.");
        }

        if (assertion->pbUserId is null || assertion->cbUserId == 0)
        {
            throw new WebAuthnLoginException(
                "The selected passkey did not return a user handle.");
        }

        ReadOnlySpan<byte> credentialId = new(
            assertion->Credential.pbId,
            checked((int)assertion->Credential.cbId));

        return new WebAuthnLoginAssertionResponseRequest
        {
            Id = Base64Url.EncodeToString(credentialId),
            RawId = Copy(assertion->Credential.pbId, assertion->Credential.cbId),
            Type = PublicKeyCredentialType,

            Response = new WebAuthnLoginAssertionResponseData
            {
                AuthenticatorData = Copy(
                    assertion->pbAuthenticatorData,
                    assertion->cbAuthenticatorData),

                Signature = Copy(
                    assertion->pbSignature,
                    assertion->cbSignature),

                ClientDataJson = clientDataJson,

                UserHandle = Copy(
                    assertion->pbUserId,
                    assertion->cbUserId)
            }
        };
    }

    private static byte[] Copy(byte* pointer, uint length)
    {
        if (pointer is null || length == 0)
        {
            return [];
        }

        byte[] result = new byte[checked((int)length)];
        Marshal.Copy((nint)pointer, result, 0, result.Length);
        return result;
    }
}