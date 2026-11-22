using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentSessionBindingTests
{
    [Fact]
    public void TryParseAndVerify_accepts_a_signed_binding_and_preserves_its_fields()
    {
        byte[] sessionId = [0x01, 0x02, 0x03];
        (byte[] payload, byte[] hostKeyBlob) = SshAgentTestData.EncodeSessionBinding(sessionId);

        Assert.True(SshAgentSessionBinding.TryParseAndVerify(payload, out var binding));
        Assert.Equal(sessionId, binding.SessionId.ToArray());
        Assert.Equal(hostKeyBlob, binding.HostKeyBlob.ToArray());
    }

    [Fact]
    public void TryParseAndVerify_rejects_a_wrong_signature()
    {
        byte[] payload = SshAgentTestData.EncodeSessionBinding([0x11, 0x22]).Payload;
        payload[^2] ^= 0x01;

        Assert.False(SshAgentSessionBinding.TryParseAndVerify(payload, out _));
    }

    [Fact]
    public void TryParseAndVerify_rejects_a_session_id_that_does_not_match_the_signature()
    {
        (byte[] payload, byte[] hostKeyBlob) = SshAgentTestData.EncodeSessionBinding([0x11, 0x22]);
        int sessionIdOffset = SshAgentTestData.EncodeString(hostKeyBlob).Length + sizeof(uint);
        payload[sessionIdOffset] ^= 0x01;

        Assert.False(SshAgentSessionBinding.TryParseAndVerify(payload, out _));
    }

    [Fact]
    public void TryParseAndVerify_rejects_an_unsupported_host_key_algorithm_label()
    {
        byte[] payload = SshAgentTestData.EncodeSessionBinding([0x11], "ssh-rsa").Payload;

        Assert.False(SshAgentSessionBinding.TryParseAndVerify(payload, out _));
    }

    [Theory]
    [MemberData(nameof(MalformedBindings))]
    public void TryParseAndVerify_rejects_truncated_or_trailing_fields(byte[] payload)
    {
        Assert.False(SshAgentSessionBinding.TryParseAndVerify(payload, out _));
    }

    public static TheoryData<byte[]> MalformedBindings
    {
        get
        {
            byte[] valid = SshAgentTestData.EncodeSessionBinding([0x11, 0x22]).Payload;
            return new TheoryData<byte[]>
            {
                { valid[..^1] },
                { SshAgentTestData.Concat(valid, [0xFF]) }
            };
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(127, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void TryParseAndVerify_checks_session_id_length_boundaries(int length, bool expected)
    {
        byte[] sessionId = Enumerable.Repeat((byte)0x31, length).ToArray();
        byte[] payload = SshAgentTestData.EncodeSessionBinding(sessionId).Payload;

        Assert.Equal(expected, SshAgentSessionBinding.TryParseAndVerify(payload, out var binding));
        if (expected)
            Assert.Equal(sessionId, binding.SessionId.ToArray());
    }
}
