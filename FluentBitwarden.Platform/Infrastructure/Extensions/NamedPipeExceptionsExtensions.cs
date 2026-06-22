namespace FluentBitwarden.Platform.Infrastructure.Extensions;

public static class NamedPipeExceptionsExtensions
{
    public static bool IsNamedPipeClientDisconnect(this IOException exception)
    {
        return exception.HResult == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_BROKEN_PIPE)
               || exception.HResult == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_NO_DATA)
               || exception.HResult == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_PIPE_NOT_CONNECTED);
    }
}