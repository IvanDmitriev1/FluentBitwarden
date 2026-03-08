namespace BitwaredApi.Abstractions;

public interface IEnvironmentConfig
{
    BitwardenEnvironment Current { get; }

    void Set(BitwardenEnvironment environment);
}
