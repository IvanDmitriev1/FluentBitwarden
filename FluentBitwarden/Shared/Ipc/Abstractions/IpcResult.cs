using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Shared.Ipc.Abstractions;

public readonly struct IpcResult<T>
    where T : notnull
{
    private IpcResult(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success => Value is not null;

    public T? Value { get; }
    public string? Error { get; }

    public static IpcResult<T> Ok(T value) => new(value, null);

    public static IpcResult<T> Fail(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("IPC error message cannot be empty.", nameof(message));

        return new IpcResult<T>(default, message);
    }

    public static implicit operator IpcResult<T>(T value) => Ok(value);
}
