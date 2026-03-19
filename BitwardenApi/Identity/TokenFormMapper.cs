using System.Globalization;

namespace BitwardenApi.Identity;

internal static class TokenFormMapper
{
    public static IReadOnlyDictionary<string, string> CreatePasswordGrant(PasswordLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.ClientId, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.MasterPasswordHash;
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreatePasswordWithTwoFactorGrant(PasswordTwoFactorLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.ClientId, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.MasterPasswordHash;
        form["twoFactorToken"] = request.TwoFactor.Code;
        form["twoFactorProvider"] = ((int)request.TwoFactor.Provider).ToString(CultureInfo.InvariantCulture);
        form["twoFactorRemember"] = request.TwoFactor.Remember ? "1" : "0";
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateRefreshTokenGrant(RefreshLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.ClientId, request.Context);
        form["grant_type"] = "refresh_token";
        form["refresh_token"] = request.RefreshToken.Value;
        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateDeviceGrant(DeviceLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.ClientId, request.Context);
        form["grant_type"] = "password";
        form["username"] = request.Email;
        form["password"] = request.OneTimeAccessCode;

        if (request.AuthRequestId is { } authRequestId && !string.IsNullOrWhiteSpace(authRequestId.Value))
        {
            form["authRequest"] = authRequestId.Value;
        }

        return form;
    }

    public static IReadOnlyDictionary<string, string> CreateClientCredentialsGrant(ClientCredentialsLoginRequest request)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = request.Scope,
            ["client_id"] = request.ClientId.Value,
            ["client_secret"] = request.ClientSecret.Value,
        };
    }

    public static IReadOnlyDictionary<string, string> CreateAuthorizationCodeGrant(AuthorizationCodeLoginRequest request)
    {
        Dictionary<string, string> form = CreateBaseDeviceForm(request.Scope, request.ClientId, request.Context);
        form["grant_type"] = "authorization_code";
        form["code"] = request.Code;
        form["redirect_uri"] = request.RedirectUri;
        form["code_verifier"] = request.CodeVerifier;
        return form;
    }

    private static Dictionary<string, string> CreateBaseDeviceForm(string scope, string clientId, BitwardenClientContext context) =>
        new(StringComparer.Ordinal)
        {
            ["scope"] = scope,
            ["client_id"] = clientId,
            ["deviceType"] = ((int)context.DeviceInfo.DeviceType).ToString(CultureInfo.InvariantCulture),
            ["deviceName"] = context.DeviceInfo.DeviceName.Value,
            ["deviceIdentifier"] = context.DeviceInfo.DeviceIdentifier.Value,
        };
}
