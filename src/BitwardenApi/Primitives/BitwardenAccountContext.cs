namespace BitwardenApi.Primitives;

public readonly record struct BitwardenAccountContext(
    UserId UserId,
    BitwardenEnvironment Environment);
