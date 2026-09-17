using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class TestMessageTypes
{
    public const ushort Echo = 40_001;
    public const ushort RequestCommand = 40_002;
    public const ushort CommandResponse = 40_003;
    public const ushort Command = 40_004;
}

[MemoryPack.MemoryPackable]
public readonly partial record struct EchoRequest(int Number, string Text) : IIpcRequestMessage
{
    public static ushort MessageType => TestMessageTypes.Echo;
}

[MemoryPack.MemoryPackable]
public readonly partial record struct CommandRequest(string Operation, int Count) : IIpcRequestMessage
{
    public static ushort MessageType => TestMessageTypes.RequestCommand;
}

[MemoryPack.MemoryPackable]
public readonly partial record struct EchoResponse(int Number, string Text, int Count);
