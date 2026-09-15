using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Infrastructure;

internal readonly struct OperationResult<TOutcome, TPayload>
    where TOutcome : notnull
    where TPayload : notnull
{
    private OperationResult(
        TOutcome outcome,
        TPayload? payload)
    {
        Outcome = outcome;
        _payload = payload;
    }

    private readonly TPayload? _payload;

    public TOutcome Outcome { get; }

    public bool TryGetPayload([NotNullWhen(true)] out TPayload? payload)
    {
        payload = _payload;
        return payload is not null;
    }

    public static OperationResult<TOutcome, TPayload> WithoutPayload(TOutcome outcome) =>
        new(outcome, default);
    public static OperationResult<TOutcome, TPayload> WithPayload(TOutcome outcome, TPayload payload) =>
        new(outcome, payload);
}