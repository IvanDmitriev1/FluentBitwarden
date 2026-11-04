using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Infrastructure.UserDialog;

[MemoryPackable]
public readonly partial struct UserDialogResult<TResult>
    where TResult : notnull
{
    [MemoryPackConstructor]
    private UserDialogResult(bool isCompleted, TResult value)
    {
        IsCompleted = isCompleted;
        Value = value;
    }

    [MemoryPackInclude]
    private bool IsCompleted { get; }

    [MemoryPackInclude]
    private TResult Value { get; }

    public static UserDialogResult<TResult> Completed(TResult value) => new(true, value);

    public static UserDialogResult<TResult> Cancelled => default;

    public bool TryGetResult(
        [MaybeNullWhen(false)] out TResult result)
    {
        result = Value;
        return IsCompleted;
    }

    public TResult GetResultOrThrow()
    {
        if (!IsCompleted)
            throw new OperationCanceledException("User dialog was cancelled");

        return Value;
    }
}
