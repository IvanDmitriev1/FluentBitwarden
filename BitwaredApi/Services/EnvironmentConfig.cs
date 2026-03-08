using BitwaredApi.Abstractions;

namespace BitwaredApi.Services;

public sealed class EnvironmentConfig : IEnvironmentConfig
{
    private readonly object _gate = new();
    private BitwardenEnvironment _current;

    public EnvironmentConfig(BitwardenEnvironment environment)
    {
        _current = environment;
    }

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
        ArgumentNullException.ThrowIfNull(env);

        lock (_gate)
        {
            _current = env;
        }
    }
}
