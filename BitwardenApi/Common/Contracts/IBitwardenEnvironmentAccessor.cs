using BitwardenApi.Models;

namespace BitwardenApi.Contracts;

public interface IBitwardenEnvironmentAccessor
{
    BitwardenEnvironment CurrentEnvironment { get; }
}
