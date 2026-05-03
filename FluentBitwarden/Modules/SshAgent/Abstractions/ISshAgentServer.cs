namespace FluentBitwarden.Modules.SshAgent.Abstractions;

public interface ISshAgentServer
{
    Task RunAsync();
}