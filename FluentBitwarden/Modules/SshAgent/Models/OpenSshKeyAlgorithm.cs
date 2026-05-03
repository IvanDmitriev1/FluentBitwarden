namespace FluentBitwarden.Modules.SshAgent.Models;

internal enum OpenSshKeyAlgorithm
{
    Ed25519
}

internal static class OpenSshKeyAlgorithmExtensions
{
    private const string SshEd25519 = "ssh-ed25519";
    private const string SshRsa = "ssh-rsa";
    private const string RsaSha2_256 = "rsa-sha2-256";
    private const string RsaSha2_512 = "rsa-sha2-512";

    public static bool TryParse(ReadOnlySpan<char> key, out OpenSshKeyAlgorithm algorithm)
    {
        if (key.SequenceEqual(SshEd25519))
        {
            algorithm = OpenSshKeyAlgorithm.Ed25519;
            return true;
        }

        algorithm = default;
        return false;
    }

    public static string ToStringFast(this OpenSshKeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            OpenSshKeyAlgorithm.Ed25519 => SshEd25519,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
}