namespace BitwardenApi.Client;

public readonly record struct BitwardenAccountContext(
    UserId UserId,
    BitwardenEnvironment Environment);
