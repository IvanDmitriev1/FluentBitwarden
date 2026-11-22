using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentDestinationResolverTests
{
    private static readonly byte[] ClientKeyBlob = SshAgentTestData.CreateKey().PublicKeyBlob;

    [Fact]
    public void TryResolve_returns_null_for_invalid_authentication_without_resolving_a_host()
    {
        int calls = 0;
        var request = new SshAgentSignRequest(ClientKeyBlob, ReadOnlyMemory<byte>.Empty, SshAgentSignatureFlags.None);
        SshAgentSessionBinding binding = CreateBinding([0x11, 0x22], out _);

        string? destination = SshAgentDestinationResolver.TryResolve(request, binding, _ =>
        {
            calls++;
            return "build.example";
        });

        Assert.Null(destination);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryResolve_returns_username_without_a_binding()
    {
        byte[] auth = CreateAuthentication("publickey", [0x11, 0x22]);
        var request = CreateRequest(auth);
        int calls = 0;

        string? destination = SshAgentDestinationResolver.TryResolve(request, null, _ =>
        {
            calls++;
            return "build.example";
        });

        Assert.Equal("alice", destination);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryResolve_returns_username_without_resolving_when_direct_session_ids_differ()
    {
        byte[] auth = CreateAuthentication("publickey", [0x11, 0x22]);
        SshAgentSessionBinding binding = CreateBinding([0x33, 0x44], out _);
        var request = CreateRequest(auth);
        int calls = 0;

        string? destination = SshAgentDestinationResolver.TryResolve(request, binding, _ =>
        {
            calls++;
            return "build.example";
        });

        Assert.Equal("alice", destination);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryResolve_returns_username_without_resolving_when_host_bound_key_differs()
    {
        byte[] auth = CreateAuthentication(
            "publickey-hostbound-v00@openssh.com", [0x11, 0x22], SshAgentTestData.CreateHostKeyBlob(0x72));
        SshAgentSessionBinding binding = CreateBinding([0x11, 0x22], out _);
        var request = CreateRequest(auth);
        int calls = 0;

        string? destination = SshAgentDestinationResolver.TryResolve(request, binding, _ =>
        {
            calls++;
            return "build.example";
        });

        Assert.Equal("alice", destination);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryResolve_uses_the_username_when_a_matching_host_is_unknown()
    {
        byte[] auth = CreateAuthentication("publickey", [0x11, 0x22]);
        SshAgentSessionBinding binding = CreateBinding([0x11, 0x22], out byte[] expectedHostKey);
        int calls = 0;
        ReadOnlyMemory<byte> receivedHostKey = default;

        string? destination = SshAgentDestinationResolver.TryResolve(CreateRequest(auth), binding, hostKey =>
        {
            calls++;
            receivedHostKey = hostKey;
            return null;
        });

        Assert.Equal("alice", destination);
        Assert.Equal(1, calls);
        Assert.Equal(expectedHostKey, receivedHostKey.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryResolve_includes_a_known_host_for_matching_ordinary_and_host_bound_auth(bool hostBound)
    {
        (byte[] signedBinding, byte[] expectedHostKey) = SshAgentTestData.EncodeSessionBinding([0x11, 0x22]);
        Assert.True(SshAgentSessionBinding.TryParseAndVerify(signedBinding, out var binding));
        byte[] auth = CreateAuthentication(
            hostBound ? "publickey-hostbound-v00@openssh.com" : "publickey",
            [0x11, 0x22],
            hostBound ? expectedHostKey : null);
        ReadOnlyMemory<byte> receivedHostKey = default;
        int calls = 0;

        string? destination = SshAgentDestinationResolver.TryResolve(CreateRequest(auth), binding, hostKey =>
        {
            calls++;
            receivedHostKey = hostKey;
            using var reader = new StringReader(
                $"build.example ssh-ed25519 {Convert.ToBase64String(hostKey.Span)}");
            return SshAgentKnownHostsResolver.Resolve(hostKey, reader);
        });

        Assert.Equal("alice@build.example", destination);
        Assert.Equal(1, calls);
        Assert.Equal(expectedHostKey, receivedHostKey.ToArray());
    }

    private byte[] CreateAuthentication(string method, byte[] sessionId, byte[]? hostKey = null) =>
        SshAgentTestData.EncodeUserAuthRequest(sessionId, "alice"u8, method, ClientKeyBlob, hostKey);

    private SshAgentSignRequest CreateRequest(byte[] auth) =>
        new(ClientKeyBlob, auth, SshAgentSignatureFlags.None);

    private SshAgentSessionBinding CreateBinding(byte[] sessionId, out byte[] hostKeyBlob)
    {
        (byte[] payload, hostKeyBlob) = SshAgentTestData.EncodeSessionBinding(sessionId);
        Assert.True(SshAgentSessionBinding.TryParseAndVerify(payload, out var binding));
        return binding;
    }
}
