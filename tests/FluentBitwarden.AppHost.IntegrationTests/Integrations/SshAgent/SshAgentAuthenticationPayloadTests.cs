using System.Text;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentAuthenticationPayloadTests
{
    private static readonly byte[] ClientKeyBlob = SshAgentTestData.CreateKey().PublicKeyBlob;
    private static readonly byte[] HostKeyBlob = SshAgentTestData.CreateHostKeyBlob();

    [Fact]
    public void TryParse_returns_exact_ordinary_authentication_fields()
    {
        byte[] sessionId = [0x01, 0x02];
        byte[] payloadData = SshAgentTestData.EncodeUserAuthRequest(
            sessionId, "alice"u8, "publickey", ClientKeyBlob);

        Assert.True(SshAgentAuthenticationPayload.TryParse(payloadData, ClientKeyBlob, out var payload));
        Assert.Equal(sessionId, payload.SessionId.ToArray());
        Assert.Equal("alice", payload.Username);
        Assert.False(payload.IsHostBound);
        Assert.Empty(payload.HostKeyBlob.ToArray());
    }

    [Fact]
    public void TryParse_returns_exact_host_bound_authentication_fields()
    {
        byte[] sessionId = [0x03, 0x04];
        byte[] payloadData = SshAgentTestData.EncodeUserAuthRequest(
            sessionId, "alice"u8, "publickey-hostbound-v00@openssh.com", ClientKeyBlob, HostKeyBlob);

        Assert.True(SshAgentAuthenticationPayload.TryParse(payloadData, ClientKeyBlob, out var payload));
        Assert.Equal(sessionId, payload.SessionId.ToArray());
        Assert.Equal("alice", payload.Username);
        Assert.True(payload.IsHostBound);
        Assert.Equal(HostKeyBlob, payload.HostKeyBlob.ToArray());
    }

    [Fact]
    public void TryParse_rejects_a_requested_client_key_mismatch()
    {
        byte[] differentClientKey = SshAgentTestData.CreateKey(0x42).PublicKeyBlob;
        byte[] data = SshAgentTestData.EncodeUserAuthRequest(
            [0x01], "alice"u8, "publickey", differentClientKey);

        Assert.False(SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    [Fact]
    public void TryParse_rejects_host_bound_authentication_without_a_host_key()
    {
        byte[] data = SshAgentTestData.EncodeUserAuthRequest(
            [0x01], "alice"u8, "publickey-hostbound-v00@openssh.com", ClientKeyBlob);

        Assert.False(SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    [Fact]
    public void TryParse_rejects_invalid_utf8_username_bytes()
    {
        byte[] data = CreatePayload(username: [0xFF]);

        Assert.False(SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    [Theory]
    [MemberData(nameof(InvalidUsernames))]
    public void TryParse_rejects_whitespace_or_formatted_usernames(byte[] username)
    {
        byte[] data = CreatePayload(username);

        Assert.False(SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    public static TheoryData<byte[]> InvalidUsernames => new()
    {
        { "alice smith"u8.ToArray() },
        { "alice\ttest"u8.ToArray() },
        { "alice@example.test"u8.ToArray() },
        { Encoding.UTF8.GetBytes("alice\u200D") }
    };

    [Theory]
    [MemberData(nameof(MalformedPayloads))]
    public void TryParse_rejects_truncated_or_trailing_fields(byte[] data)
    {
        Assert.False(SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    public static TheoryData<byte[]> MalformedPayloads
    {
        get
        {
            byte[] valid = SshAgentTestData.EncodeUserAuthRequest(
                [0x01, 0x02], "alice"u8, "publickey", ClientKeyBlob);
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
    public void TryParse_checks_session_id_length_boundaries(int length, bool expected)
    {
        byte[] sessionId = Enumerable.Repeat((byte)0x21, length).ToArray();
        byte[] data = CreatePayload("alice"u8.ToArray(), sessionId);

        Assert.Equal(expected, SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out var payload));
        if (expected)
            Assert.Equal(sessionId, payload.SessionId.ToArray());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(255, true)]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public void TryParse_checks_ascii_username_byte_length_boundaries(int length, bool expected)
    {
        byte[] username = Enumerable.Repeat((byte)'a', length).ToArray();
        byte[] data = CreatePayload(username);

        Assert.Equal(expected, SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out _));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void TryParse_checks_multibyte_username_byte_limits(bool overLimit, bool expected)
    {
        string username = new string('\u00E9', 128) + (overLimit ? "a" : string.Empty);
        byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
        Assert.Equal(overLimit ? 257 : 256, usernameBytes.Length);
        byte[] data = CreatePayload(usernameBytes);

        bool parsed = SshAgentAuthenticationPayload.TryParse(data, ClientKeyBlob, out var payload);
        Assert.Equal(expected, parsed);
        if (expected)
            Assert.Equal(username, payload.Username);
    }

    private byte[] CreatePayload(byte[] username, byte[]? sessionId = null) =>
        SshAgentTestData.EncodeUserAuthRequest(
            sessionId ?? [0x01, 0x02], username, "publickey", ClientKeyBlob);
}
