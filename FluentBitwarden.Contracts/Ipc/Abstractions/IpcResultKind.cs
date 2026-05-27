namespace FluentBitwarden.Contracts.Ipc.Abstractions;

public enum IpcResultKind : byte
{
    None = 0,
    Success = 1,
    Failure = 2
}

internal static class IpcResultKindExtensions
{
    extension(IpcResultKind kind)
    {
        public void ValidateState<TValue>(TValue? valueOrDefault, string? error)
        {
            switch (kind)
            {
                case IpcResultKind.Success:
                {
                    if (valueOrDefault is null)
                    {
                        throw new ArgumentException(
                            "Successful IPC result must contain a value.",
                            nameof(valueOrDefault));
                    }

                    if (error is null)
                    {
                        throw new ArgumentException(
                            "Successful IPC result cannot contain an error.",
                            nameof(error));
                    }

                    break;
                }
                case IpcResultKind.Failure:
                {
                    if (error is null)
                    {
                        throw new ArgumentException(
                            "Failed IPC result must contain an error.",
                            nameof(error));
                    }

                    break;
                }
                case IpcResultKind.None:
                default:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unknown IPC result kind.");
                }
            }
        }

        public void ValidateState(string? error)
        {
            switch (kind)
            {
                case IpcResultKind.Success:
                {
                    if (error is null)
                    {
                        throw new ArgumentException(
                            "Success result cannot contain an error.",
                            nameof(error));
                    }

                    break;
                }

                case IpcResultKind.Failure:
                {
                    if (error is null)
                    {
                        throw new ArgumentException("Failure result must contain an error.", nameof(error));
                    }

                    break;
                }

                case IpcResultKind.None:
                default:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unknown IPC result kind.");
                }
            }

        }
    }
}