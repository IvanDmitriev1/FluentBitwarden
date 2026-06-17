namespace BitwardenApi.Infrastructure.Transport;

public interface IBitwardenEnvironmentAccessor
{
    BitwardenEnvironment CurrentEnvironment { get; }
}
