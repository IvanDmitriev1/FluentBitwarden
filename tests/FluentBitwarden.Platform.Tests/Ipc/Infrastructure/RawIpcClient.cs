using System.Buffers.Binary;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class RawIpcClient
{
    private const int RequestHeaderSize = sizeof(ushort) + sizeof(ushort) + sizeof(int);
    private const int ResponseHeaderSize = sizeof(ushort) + sizeof(int);

    public static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
    {
        var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
            await pipe.ConnectAsync((int)IpcTestHost.Timeout.TotalMilliseconds);
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
        ReadOnlyMemory<byte> payload = default)
    {
        byte[] header = RequestHeader(version, messageType, payload.Length);
        await pipe.WriteAsync(header);
        if (!payload.IsEmpty)
            await pipe.WriteAsync(payload);
        await pipe.FlushAsync();
    }

    public static async Task<Exception> SendMalformedRequestAsync(
        string pipeName,
        ReadOnlyMemory<byte> request,
        bool closeAfterWrite)
    {
        await using var pipe = await ConnectAsync(pipeName);
        Task<Exception> responseRead = ReadResponseFailureAsync(pipe);

        await pipe.WriteAsync(request);
        await pipe.FlushAsync();

        if (closeAfterWrite)
            await pipe.DisposeAsync();

        return await responseRead.WaitAsync(IpcTestHost.Timeout);
    }

    private static async Task<Exception> ReadResponseFailureAsync(Stream pipe)
    {
        try
        {
            byte[] header = new byte[ResponseHeaderSize];
            await pipe.ReadExactlyAsync(header);
            return new InvalidOperationException("The server unexpectedly sent a response.");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
