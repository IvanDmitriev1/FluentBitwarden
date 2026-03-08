using System.Collections.Specialized;

namespace BitwaredApi.Models.Auth;

public abstract record TokenRequestModel(
    ClientType ClientType,
    DeviceType DeviceType,
    string DeviceName,
    string DeviceIdentifier,
    string? TwoFactorToken = null,
    TwoFactorProviderType? TwoFactorProvider = null,
    bool TwoFactorRemember = false,
    string? AuthRequestId = null)
{
    public virtual IReadOnlyDictionary<string, string> ToFormValues()
    {
        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["scope"] = "api offline_access",
            ["client_id"] = ClientType.ToClientId(),
            ["deviceType"] = ((int)DeviceType).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["deviceName"] = DeviceName,
            ["deviceIdentifier"] = DeviceIdentifier,
        };

        if (!string.IsNullOrWhiteSpace(AuthRequestId))
        {
            form["authRequest"] = AuthRequestId;
        }

        if (!string.IsNullOrWhiteSpace(TwoFactorToken) && TwoFactorProvider is not null)
        {
            form["twoFactorToken"] = TwoFactorToken;
            form["twoFactorProvider"] = ((int)TwoFactorProvider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            form["twoFactorRemember"] = TwoFactorRemember ? "1" : "0";
        }

        return form;
    }
}

public sealed record PasswordTokenRequestModel(
    string Username,
    string AuthorizationHash,
    ClientType ClientType,
    DeviceType DeviceType,
    string DeviceName,
    string DeviceIdentifier,
    string? TwoFactorToken = null,
    TwoFactorProviderType? TwoFactorProvider = null,
    bool TwoFactorRemember = false,
    string? AuthRequestId = null)
    : TokenRequestModel(
        ClientType,
        DeviceType,
        DeviceName,
        DeviceIdentifier,
        TwoFactorToken,
        TwoFactorProvider,
        TwoFactorRemember,
        AuthRequestId)
{
    public override IReadOnlyDictionary<string, string> ToFormValues()
    {
        Dictionary<string, string> form = new(base.ToFormValues(), StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["username"] = Username,
            ["password"] = AuthorizationHash,
        };

        return form;
    }
}

public sealed record RefreshTokenRequestModel(
    string RefreshToken,
    ClientType ClientType,
    DeviceType DeviceType,
    string DeviceName,
    string DeviceIdentifier)
    : TokenRequestModel(
        ClientType,
        DeviceType,
        DeviceName,
        DeviceIdentifier)
{
    public override IReadOnlyDictionary<string, string> ToFormValues()
    {
        Dictionary<string, string> form = new(base.ToFormValues(), StringComparer.Ordinal)
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = RefreshToken,
        };

        return form;
    }
}

public static class ClientTypeExtensions
{
    public static string ToClientId(this ClientType clientType)
        => clientType switch
        {
            ClientType.Desktop => "desktop",
            _ => "desktop",
        };
}
