using System.Buffers.Binary;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class RawIpcClient
{
    private const int RequestHeaderSize = sizeof(ushort) + sizeof(ushort) + sizeof(int);

    public static async Task<NamedPipeClientStream> ConnectAsync(
        string pipeName,
        CancellationToken cancellationToken)
    {
        var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
            await pipe.ConnectAsync(IpcTestHost.TimeoutMilliseconds, cancellationToken);
            return pipe;
        }
        catch
        {
            await pipe.DisposeAsync();
            throw;
        }
    }

    public static byte[] RequestHeader(
        ushort version,
        ushort messageType,
        int payloadLength)
    {
        byte[] header = new byte[RequestHeaderSize];
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0, sizeof(ushort)), version);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(sizeof(ushort), sizeof(ushort)), messageType);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(sizeof(ushort) * 2, sizeof(int)), payloadLength);
        return header;
    }

    public static async Task WriteRequestAsync(
        Stream pipe,
        ushort version,
        ushort messageType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        byte[] header = RequestHeader(version, messageType, payload.Length);
        await pipe.WriteAsync(header, cancellationToken);
        if (!payload.IsEmpty)
            await pipe.WriteAsync(payload, cancellationToken);
        await pipe.FlushAsync(cancellationToken);
    }

    public static async Task SendRequestExpectingServerDisconnectAsync(
        string pipeName,
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken)
    {
        await using var pipe = await ConnectAsync(pipeName, cancellationToken);
        Task responseRead = ReadServerDisconnectAsync(pipe, cancellationToken);

        await pipe.WriteAsync(request, cancellationToken);
        await pipe.FlushAsync(cancellationToken);

        await responseRead.WaitAsync(IpcTestHost.Timeout, cancellationToken);
    }

    public static async Task SendPartialRequestAndDisconnectAsync(
        string pipeName,
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken)
    {
        await using var pipe = await ConnectAsync(pipeName, cancellationToken);

        await pipe.WriteAsync(request, cancellationToken);
        await pipe.FlushAsync(cancellationToken);
    }

    private static async Task ReadServerDisconnectAsync(
        Stream pipe,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] responseStart = new byte[1];
            await pipe.ReadExactlyAsync(responseStart, cancellationToken);
        }
        catch (IOException)
        {
            return;
        }

        throw new InvalidOperationException("The server unexpectedly sent a response.");
    }
}
