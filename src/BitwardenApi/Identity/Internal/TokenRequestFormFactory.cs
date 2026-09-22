using System.Globalization;

namespace BitwardenApi.Identity.Internal;

internal static class TokenRequestFormFactory
{
    public static IReadOnlyDictionary<string, string> CreatePasswordGrant(this PasswordAuthenticationRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.MasterPasswordHash;
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreatePasswordWithTwoFactorGrant(this PasswordTwoFactorAuthenticationRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.MasterPasswordHash;
        form["twoFactorToken"] = request.TwoFactor.Code;
        form["twoFactorProvider"] = ((int)request.TwoFactor.Provider).ToString(CultureInfo.InvariantCulture);
        form["twoFactorRemember"] = "1";
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateWebAuthnGrant(this WebAuthnAuthenticationRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "webauthn";
        form["token"] = request.Token.Value;
        form["deviceResponse"] = JsonSerializer.Serialize(
            request.DeviceResponse,
            IdentityJsonContext.ConfiguredDefault.WebAuthnLoginAssertionResponseRequest);
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateRefreshTokenGrant(this RefreshAuthenticationRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "refresh_token";
        form["refresh_token"] = request.SessionRefreshToken.Value;
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateDeviceGrant(this DeviceAuthenticationRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.OneTimeAccessCode;

        if (request.AuthRequestId is { } authRequestId && !string.IsNullOrWhiteSpace(authRequestId.Value))
        {
            form["authRequest"] = authRequestId.Value;
        }

        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateAuthorizationCodeGrant(this AuthorizationCodeLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.Context);
        form["grant_type"] = "authorization_code";
        form["code"] = request.Code;
        form["redirect_uri"] = request.RedirectUri;
        form["code_verifier"] = request.CodeVerifier;
        return form;
    }

    private static Dictionary<string, string> CreateBaseDeviceForm(string scope, BitwardenClientContext context) =>
        new(StringComparer.Ordinal)
        {
            ["scope"] = scope,
            ["client_id"] = "desktop",
            ["deviceType"] = "6", //WindowsDesktop  
            ["deviceName"] = context.DeviceInfo.DeviceName.Value,
            ["deviceIdentifier"] = context.DeviceInfo.DeviceIdentifier.Value,
        };
}

