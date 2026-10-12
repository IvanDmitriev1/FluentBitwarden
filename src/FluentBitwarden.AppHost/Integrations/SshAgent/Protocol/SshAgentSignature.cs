using System.Text;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal sealed record SshAgentSignature(string Algorithm, byte[] Value)
{
    public long BlobLength { get; } = 2L * sizeof(uint) + Encoding.UTF8.GetByteCount(Algorithm) + Value.Length;
}
