using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentMessagesHandlerTests
{
    [Fact]
    public async Task RequestIdentities_encodes_each_key_and_utf8_comment()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var identity = new SshAgentIdentity(new byte[] { 0x01, 0x02 }, "key café");
        keyProvider.GetIdentitiesAsync(Arg.Any<CancellationToken>()).Returns((SshAgentIdentity[])[identity]);
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty),
            new SshAgentConnectionState(),
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
        keyProvider.GetIdentitiesAsync(Arg.Any<CancellationToken>()).Returns(
            (SshAgentIdentity[])[new SshAgentIdentity(ReadOnlyMemory<byte>.Empty, new string('x', SshAgentMessage.MaxLength))]);
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty),
            new SshAgentConnectionState(),
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
                Arg.Any<SshAgentSessionBinding?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(signature));
        var handler = new SshAgentMessagesHandler(keyProvider);
        byte[] requestPayload = SshAgentTestData.EncodeSignRequest([0xAA], [0xBB, 0xCC], flags);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest, requestPayload),
            new SshAgentConnectionState(),
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
            Arg.Any<SshAgentSessionBinding?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SessionBind_keeps_the_first_binding_after_a_rejected_rebind()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        SshAgentSessionBinding? receivedBinding = null;
        bool? observedForwarding = null;
        keyProvider.GetSignatureAsync(
                Arg.Any<SshAgentSignRequest>(),
                Arg.Do<SshAgentSessionBinding?>(binding => receivedBinding = binding),
                Arg.Do<bool>(isForwarded => observedForwarding = isForwarded),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(new SshAgentSignature("ssh-ed25519", [0x01])));
        var handler = new SshAgentMessagesHandler(keyProvider);
        var connection = new SshAgentConnectionState();
        byte[] firstSessionId = [0x11, 0x22];
        byte[] secondSessionId = [0x33, 0x44];
        (byte[] firstBindingData, byte[] firstHostKey) = SshAgentTestData.EncodeSessionBinding(
            firstSessionId, signingSeedByte: 0x61);
        (byte[] secondBindingData, byte[] secondHostKey) = SshAgentTestData.EncodeSessionBinding(
            secondSessionId, signingSeedByte: 0x72);

        SshAgentMessage firstBind = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Extension,
                SshAgentTestData.EncodeExtensionRequest("session-bind@openssh.com", firstBindingData)),
            connection, CancellationToken.None);
        SshAgentMessage secondBind = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Extension,
                SshAgentTestData.EncodeExtensionRequest("session-bind@openssh.com", secondBindingData)),
            connection, CancellationToken.None);
        SshAgentMessage signResponse = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0)),
            connection,
            CancellationToken.None);

        Assert.Equal(SshAgentMessageType.Success, firstBind.Type);
        Assert.Equal(SshAgentMessageType.ExtensionFailure, secondBind.Type);
        Assert.Equal(SshAgentMessageType.SignResponse, signResponse.Type);
        Assert.False(firstHostKey.SequenceEqual(secondHostKey));
        Assert.False(firstSessionId.SequenceEqual(secondSessionId));
        Assert.NotNull(receivedBinding);
        Assert.Equal(firstHostKey, receivedBinding?.HostKeyBlob.ToArray());
        Assert.Equal(firstSessionId, receivedBinding?.SessionId.ToArray());
        Assert.False(observedForwarding);
    }

    [Fact]
    public async Task Forwarding_notification_clears_binding_and_rejects_later_direct_bind()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        SshAgentSessionBinding? receivedBinding = new(new byte[] { 0x01 }, new byte[] { 0x02 });
        bool? observedForwarding = null;
        keyProvider.GetSignatureAsync(
                Arg.Any<SshAgentSignRequest>(),
                Arg.Do<SshAgentSessionBinding?>(binding => receivedBinding = binding),
                Arg.Do<bool>(isForwarded => observedForwarding = isForwarded),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(new SshAgentSignature("ssh-ed25519", [0x01])));
        var handler = new SshAgentMessagesHandler(keyProvider);
        var connection = new SshAgentConnectionState();
        (byte[] bindingData, _) = SshAgentTestData.EncodeSessionBinding([0x11, 0x22]);
        (byte[] rejectedBindingData, _) = SshAgentTestData.EncodeSessionBinding([0x33, 0x44]);
        SshAgentMessage directBind = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Extension,
                SshAgentTestData.EncodeExtensionRequest("session-bind@openssh.com", bindingData)),
            connection, CancellationToken.None);
        SshAgentMessage forwardingNotification = await handler.HandleAsync(
            SshAgentTestData.CreateForwardingNotification(),
            connection, CancellationToken.None);
        SshAgentMessage laterBind = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.Extension,
                SshAgentTestData.EncodeExtensionRequest("session-bind@openssh.com", rejectedBindingData)),
            connection, CancellationToken.None);
        SshAgentMessage signResponse = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0)),
            connection,
            CancellationToken.None);

        Assert.Equal(SshAgentMessageType.Success, directBind.Type);
        Assert.Equal(SshAgentMessageType.ExtensionFailure, forwardingNotification.Type);
        Assert.Equal(SshAgentMessageType.ExtensionFailure, laterBind.Type);
        Assert.Equal(SshAgentMessageType.SignResponse, signResponse.Type);
        Assert.Null(receivedBinding);
        Assert.True(observedForwarding);
    }

    [Theory]
    [MemberData(nameof(MalformedSignRequests))]
    public async Task SignRequest_fails_on_truncated_or_trailing_payload(byte[] payload)
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(SshAgentMessageType.SignRequest, payload),
            new SshAgentConnectionState(),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
        _ = keyProvider.DidNotReceive().GetSignatureAsync(
            Arg.Any<SshAgentSignRequest>(),
            Arg.Any<SshAgentSessionBinding?>(),
            Arg.Any<bool>(),
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
            new SshAgentConnectionState(),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
        _ = keyProvider.DidNotReceive().GetSignatureAsync(
            Arg.Any<SshAgentSignRequest>(),
            Arg.Any<SshAgentSessionBinding?>(),
            Arg.Any<bool>(),
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
            new SshAgentConnectionState(),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }

    [Fact]
    public async Task SignRequest_fails_when_the_signature_response_exceeds_the_frame_limit()
    {
        var keyProvider = Substitute.For<ISshKeyProvider>();
        keyProvider.GetSignatureAsync(
                Arg.Any<SshAgentSignRequest>(),
                Arg.Any<SshAgentSessionBinding?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<SshAgentSignature?>(
                new SshAgentSignature("ssh-ed25519", new byte[SshAgentMessage.MaxLength])));
        var handler = new SshAgentMessagesHandler(keyProvider);

        SshAgentMessage response = await handler.HandleAsync(
            new SshAgentMessage(
                SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0)),
            new SshAgentConnectionState(),
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
            new SshAgentConnectionState(),
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
            new SshAgentConnectionState(),
            CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, response);
    }
}
