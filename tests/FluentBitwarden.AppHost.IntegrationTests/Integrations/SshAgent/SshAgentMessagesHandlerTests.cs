using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentMessagesHandlerTests
{
    [Fact]
    public async Task RequestIdentities_encodes_each_key_and_utf8_comment()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var identity = new SshAgentIdentity(new byte[] { 0x01, 0x02 }, "key café");
        keyProvider.GetIdentities().Returns([identity]);
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty),
            CancellationToken.None);

        byte[] expectedPayload = SshAgentTestData.Concat(
            [0, 0, 0, 1],
            SshAgentTestData.EncodeString(new byte[] { 0x01, 0x02 }),
            SshAgentTestData.EncodeString("key café"));
        Assert.Equal(SshAgentMessageType.IdentitiesAnswer, response.Type);
        Assert.Equal(expectedPayload, response.Payload.ToArray());
    }

    [Fact]
    public async Task RequestIdentities_fails_when_the_response_exceeds_the_frame_limit()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        keyProvider.GetIdentities().Returns(
            [new SshAgentIdentity(ReadOnlyMemory<byte>.Empty, new string('x', SshAgentMessage.MaxLength))]);
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData((uint)SshAgentSignatureFlags.RsaSha2256)]
    [InlineData((uint)SshAgentSignatureFlags.RsaSha2512)]
    public async Task SignRequest_encodes_the_provider_signature_and_forwards_supported_flags(uint flags)
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var signature = new SshAgentSignature("ssh-ed25519", [0xA1, 0xB2]);
        SshAgentSignRequest? receivedRequest = null;
        keyProvider.GetSignatureAsync(
                Arg.Do<SshAgentSignRequest>(request => receivedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(signature));
        var handler = new SshAgentMessagesHandler(keyProvider);
        byte[] requestPayload = SshAgentTestData.EncodeSignRequest([0xAA], [0xBB, 0xCC], flags);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest, requestPayload),
            CancellationToken.None);

        byte[] signatureBlob = SshAgentTestData.Concat(
            SshAgentTestData.EncodeString("ssh-ed25519"),
            SshAgentTestData.EncodeString(new byte[] { 0xA1, 0xB2 }));
        byte[] expectedPayload = SshAgentTestData.Concat(
            SshAgentTestData.EncodeUInt32((uint)signatureBlob.Length),
            signatureBlob);
        Assert.Equal(SshAgentMessageType.SignResponse, response.Type);
        Assert.Equal(expectedPayload, response.Payload.ToArray());
        Assert.True(receivedRequest.HasValue);
        Assert.Equal(new byte[] { 0xAA }, receivedRequest?.PublicKeyBlob.ToArray());
        Assert.Equal(new byte[] { 0xBB, 0xCC }, receivedRequest?.Data.ToArray());
        Assert.Equal((SshAgentSignatureFlags)flags, receivedRequest?.Flags);
        _ = keyProvider.Received(1).GetSignatureAsync(
            Arg.Any<SshAgentSignRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(MalformedSignRequests))]
    public async Task SignRequest_fails_on_truncated_or_trailing_payload(byte[] payload)
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest, payload),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
        _ = keyProvider.DidNotReceive().GetSignatureAsync(
            Arg.Any<SshAgentSignRequest>(),
            Arg.Any<CancellationToken>());
    }

    public static TheoryData<byte[]> MalformedSignRequests => new()
    {
        { [0, 0, 0, 4, 0xAA] },
        {
            SshAgentTestData.Concat(
                SshAgentTestData.EncodeString(new byte[] { 0xAA }),
                SshAgentTestData.EncodeString(new byte[] { 0xBB }),
                SshAgentTestData.EncodeUInt32(0),
                [0xFF])
        }
    };

    [Theory]
    [InlineData(1u)]
    [InlineData(8u)]
    [InlineData(uint.MaxValue)]
    public async Task SignRequest_rejects_unsupported_flags(uint flags)
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var handler = new SshAgentMessagesHandler(keyProvider);
        byte[] payload = SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], flags);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest, payload),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
        _ = keyProvider.DidNotReceive().GetSignatureAsync(
            Arg.Any<SshAgentSignRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignRequest_fails_when_the_key_provider_has_no_matching_key()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(
                SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0)),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }

    [Fact]
    public async Task SignRequest_fails_when_the_signature_response_exceeds_the_frame_limit()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        keyProvider.GetSignatureAsync(
                Arg.Any<SshAgentSignRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(
                new SshAgentSignature("ssh-ed25519", new byte[SshAgentMessage.MaxLength])));
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(
                SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0)),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }

    [Theory]
    [MemberData(nameof(ExtensionFailures))]
    public async Task Extension_returns_extension_failure_for_malformed_or_unsupported_requests(byte[] payload)
    {
        var handler = new SshAgentMessagesHandler(Substitute.For<ISshKeyProvider>());

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Extension, payload),
            CancellationToken.None);

        Assert.Equal(SshAgentMessageType.ExtensionFailure, response.Type);
        Assert.Empty(response.Payload.ToArray());
    }

    public static TheoryData<byte[]> ExtensionFailures => new()
    {
        { [0, 0, 0, 4, 0x71] },
        { SshAgentTestData.EncodeExtensionRequest("unknown@example.test", [0x01]) },
        { SshAgentTestData.EncodeExtensionRequest("query", ReadOnlySpan<byte>.Empty) }
    };

    [Fact]
    public async Task Handle_fails_for_an_unimplemented_message_type()
    {
        var handler = new SshAgentMessagesHandler(Substitute.For<ISshKeyProvider>());

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Success, ReadOnlyMemory<byte>.Empty),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }
}
