using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc.Abstractions;

[MemoryPackable(SerializeLayout.Explicit)]
public readonly partial struct IpcResult<TValue>
    where TValue : notnull
{
    [MemoryPackConstructor]
    public IpcResult(
        IpcResultKind kind,
        TValue? valueOrDefault,
        string? errorOrDefault)

    {
        kind.ValidateState(valueOrDefault, errorOrDefault);

        Kind = kind;
        ValueOrDefault = valueOrDefault;
        ErrorOrDefault = errorOrDefault;
    }

    [MemoryPackOrder(0)]
    public IpcResultKind Kind { get; }

    [MemoryPackOrder(1)]
    public TValue? ValueOrDefault { get; }

    [MemoryPackOrder(2)]
    public string? ErrorOrDefault { get; }

    [MemoryPackIgnore]
    [MemberNotNullWhen(true, nameof(ValueOrDefault))]
    [MemberNotNullWhen(false, nameof(ErrorOrDefault))]
    public bool IsSuccess => Kind == IpcResultKind.Success;

    [MemoryPackIgnore]
    [MemberNotNullWhen(true, nameof(ErrorOrDefault))]
    [MemberNotNullWhen(false, nameof(ValueOrDefault))]
    public bool IsFailure => Kind == IpcResultKind.Failure;

    public TValue GetValueOrThrow()
    {
        if (IsSuccess)
        {
            return ValueOrDefault;
        }

        throw new AggregateException("IPC result does not contain a success value.",
            new Exception(ErrorOrDefault));
    }

    public static IpcResult<TValue> Success(TValue value) => new(
        IpcResultKind.Success,
        value,
        null);

    public static IpcResult<TValue> Failure(string error) => new(
        IpcResultKind.Failure,
        default,
        error);
}

