using System.Text;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentIdentity(
    ReadOnlyMemory<byte> PublicKeyBlob,
    string Comment)
{
    public static readonly SshAgentIdentity Empty = new(Array.Empty<byte>(), string.Empty);

    public readonly long BlobLength = 2L * sizeof(uint) + PublicKeyBlob.Length + Encoding.UTF8.GetByteCount(Comment);
}
