namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello;

public enum WindowsHelloEnrollmentOutcome : byte
{
    Enrolled,
    Cancelled,
    Unavailable,
    Failed
}
