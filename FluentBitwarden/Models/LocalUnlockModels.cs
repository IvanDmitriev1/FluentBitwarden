namespace FluentBitwarden.Models;

public sealed record LocalUnlockStatus(
    bool IsWindowsHelloAvailable,
    bool IsWindowsHelloEnrolled,
    bool IsPinEnrolled)
{
    public bool HasAnyMethod => IsWindowsHelloEnrolled || IsPinEnrolled;
}

public sealed record UnlockEnrollmentSelection(
    bool EnableWindowsHello,
    string? Pin);
