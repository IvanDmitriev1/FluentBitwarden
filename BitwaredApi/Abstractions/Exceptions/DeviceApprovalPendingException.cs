namespace BitwaredApi.Abstractions.Exceptions;

public sealed class DeviceApprovalPendingException : Exception
{
    public DeviceApprovalPendingException(string? message = null)
        : base(message ?? "The device login request is still waiting for approval.")
    {
    }
}
