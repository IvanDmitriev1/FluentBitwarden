namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello.Models;

public enum WindowsHelloEnrollmentStatus : byte
{
    Unavailable,
    NotEnrolled,
    Enrolled
}

public enum WindowsHelloEnrollmentOutcome : byte
{
    Enrolled,
    Cancelled,
    Unavailable,
    Failed
}
