using System.Buffers;

namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeMessage<out TSelf> where TSelf : IPipeMessage<TSelf>
{
    static abstract TSelf ReadPayload(ReadOnlySpan<byte> payload);

    void WritePayload(IBufferWriter<byte> writer);
}

public interface IPipeRequest<out TSelf> : IPipeMessage<TSelf> where TSelf : IPipeRequest<TSelf>
{
    static abstract ushort MessageType { get; }
}
