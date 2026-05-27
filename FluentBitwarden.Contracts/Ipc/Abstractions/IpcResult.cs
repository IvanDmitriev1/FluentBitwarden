using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc.Abstractions;

[MemoryPackable(SerializeLayout.Explicit)]
public readonly partial struct IpcResult
{
    [MemoryPackConstructor]
    public IpcResult(IpcResultKind kind, string? errorOrDefault)
    {
        kind.ValidateState(errorOrDefault);

        Kind = kind;
        ErrorOrDefault = errorOrDefault;
    }

    [MemoryPackOrder(0)]
    public IpcResultKind Kind { get; }

    [MemoryPackOrder(1)]
    public string? ErrorOrDefault { get; }


    [MemoryPackIgnore]
    [MemberNotNullWhen(false, nameof(ErrorOrDefault))]
    public bool IsSuccess => Kind == IpcResultKind.Success;

    [MemoryPackIgnore]
    [MemberNotNullWhen(true, nameof(ErrorOrDefault))]
    public bool IsFailure => Kind == IpcResultKind.Failure;

    public bool GetValueOrThrow()
    {
        if (IsSuccess)
        {
            return Kind == IpcResultKind.Success;
        }

        throw new AggregateException("IPC result does not contain a success value.",
            new Exception(ErrorOrDefault.ToString()));
    }

    public static IpcResult Success() => new(
        IpcResultKind.Success,
        null);

    public static IpcResult Failure(string error) => new(
        IpcResultKind.Failure,
        error);
}