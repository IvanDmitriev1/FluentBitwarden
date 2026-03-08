using BitwaredApi.Abstractions;

namespace BitwaredApi.Services;

internal sealed class EnvironmentConfig(BitwardenEnvironment environment) : IEnvironmentConfig
{
    private readonly Lock _gate = new();
    private BitwardenEnvironment _current = environment;

    public BitwardenEnvironment Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Set(BitwardenEnvironment env)
    {
        lock (_gate)
        {
            _current = env;
        }
    }
}
