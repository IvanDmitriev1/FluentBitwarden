using OtpNet;

namespace FluentBitwarden.Shared.Totp;

public sealed record OtpAuthData(
    OtpType Type,
    string Secret,
    string Account,
    string? Issuer,
    OtpHashMode Algorithm,
    int Digits,
    int PeriodSeconds,
    long Counter);