using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentKnownHostsResolverTests
{
    private static readonly byte[] HostKeyBlob = SshAgentTestData.CreateHostKeyBlob();
    private static readonly string EncodedHostKey = Convert.ToBase64String(HostKeyBlob);

    [Fact]
    public void Resolve_returns_a_unique_matching_hostname()
    {
        string? result = Resolve($"build.example ssh-ed25519 {EncodedHostKey}");

        Assert.Equal("build.example", result);
    }

    [Fact]
    public void Resolve_accepts_repeated_case_equivalent_entries()
    {
        string? result = Resolve(string.Join(Environment.NewLine,
            $"Build.Example ssh-ed25519 {EncodedHostKey}",
            $"build.example ssh-ed25519 {EncodedHostKey}"));

        Assert.Equal("Build.Example", result);
    }

    [Fact]
    public void Resolve_returns_null_when_no_entry_matches_the_key()
    {
        string? result = Resolve($"other.example ssh-ed25519 {Convert.ToBase64String(SshAgentTestData.CreateHostKeyBlob(0x72))}");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_does_not_resolve_hashed_host_entries()
    {
        const string hashedHost = "|1|AAECAwQFBgcICQoLDA0ODxAREhM=|+OKs033YXQS1W2SbwiJinTQqF9k=";
        string? result = Resolve($"{hashedHost} ssh-ed25519 {EncodedHostKey}");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_returns_null_for_malformed_matching_entries()
    {
        string? result = Resolve($"bad!hostname ssh-ed25519 {EncodedHostKey}");

        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(AmbiguousEntries))]
    public void Resolve_returns_null_when_matching_entries_disagree(string entries)
    {
        Assert.Null(Resolve(entries));
    }

    public static TheoryData<string> AmbiguousEntries => new()
    {
        {
            string.Join(Environment.NewLine,
                $"build.example ssh-ed25519 {EncodedHostKey}",
                $"deploy.example ssh-ed25519 {EncodedHostKey}")
        },
        {
            string.Join(Environment.NewLine,
                $"build.example ssh-ed25519 {EncodedHostKey}",
                $"[build.example]:2222 ssh-ed25519 {EncodedHostKey}")
        }
    };

    [Fact]
    public void Resolve_ignores_an_unrelated_key_when_a_matching_entry_exists()
    {
        string entries = string.Join(Environment.NewLine,
            $"other.example ssh-ed25519 {Convert.ToBase64String(SshAgentTestData.CreateHostKeyBlob(0x72))}",
            $"build.example ssh-ed25519 {EncodedHostKey}");

        Assert.Equal("build.example", Resolve(entries));
    }

    private string? Resolve(string entries)
    {
        using var reader = new StringReader(entries);
        return SshAgentKnownHostsResolver.Resolve(HostKeyBlob, reader);
    }
}
