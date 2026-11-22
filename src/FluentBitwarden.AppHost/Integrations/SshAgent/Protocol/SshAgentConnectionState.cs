namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal sealed class SshAgentConnectionState
{
    private bool _isForwardingConnection;

    public SshAgentSessionBinding? SessionBinding { get; private set; }
    public bool IsForwardingConnection => _isForwardingConnection;

    public void MarkForwardingConnection()
    {
        _isForwardingConnection = true;
        SessionBinding = null;
    }

    public bool TryBind(SshAgentSessionBinding binding)
    {
        if (_isForwardingConnection || SessionBinding is not null)
            return false;

        SessionBinding = binding;
        return true;
    }
}
