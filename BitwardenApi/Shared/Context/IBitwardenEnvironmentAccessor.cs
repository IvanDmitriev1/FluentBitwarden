namespace BitwardenApi.Shared.Context;

public interface IBitwardenEnvironmentAccessor
{
    BitwardenEnvironment CurrentEnvironment { get; }
}
