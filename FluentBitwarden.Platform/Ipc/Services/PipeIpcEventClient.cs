using System.Collections.Concurrent;
using System.IO.Pipes;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Infrastructure.Extensions;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcEventClient(string pipeName) : BackgroundService, IIpcEventClient
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromMilliseconds(250);

    private readonly ConcurrentDictionary<ushort, IpcEventWaiter> _waiters = [];

    public async Task<TEvent> WaitAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage
    {
        using var waiter = new IpcEventWaiter<TEvent>(cancellationToken);
        if (!_waiters.TryAdd(waiter.MessageType, waiter))
        {
            throw new InvalidOperationException(
                $"An IPC event waiter for message type '{waiter.MessageType}' is already registered.");
        }

        try
        {
            return await waiter.Task;
        }
        finally
        {
            _waiters.Remove(waiter.MessageType, out _);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReadEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (
                exception is EndOfStreamException ||
                (exception is IOException ioException && ioException.IsNamedPipeClientDisconnect()))
            {
                Debug.WriteLine("IPC event connection closed; reconnecting.");
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.WriteException(exception);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }

        foreach (var pair in _waiters)
        {
            if (_waiters.TryRemove(pair))
            {
                pair.Value.TrySetCanceled();
            }
        }
    }

    private async Task ReadEventsAsync(CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.In,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        await pipe.ConnectAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            var header = await IpcMessageHeader.ReadAsync(pipe, cancellationToken);
            if (!_waiters.TryRemove(header.MessageType, out var waiter))
            {
                await IpcWireProtocol.DiscardPayloadAsync(
                    pipe,
                    header.PayloadLength,
                    cancellationToken);

                continue;
            }

            try
            {
                using var bufferOwner = MemoryOwner<byte>.Allocate(header.PayloadLength);
                await pipe.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

                waiter.Complete(bufferOwner.Span);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                waiter.TrySetCanceled();
                throw;
            }
            catch (Exception exception)
            {
                waiter.TrySetException(exception);
            }
        }
    }
}
