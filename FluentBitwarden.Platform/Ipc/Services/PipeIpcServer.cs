using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcServer : BackgroundService
{
    private readonly int _maxConCurrentConnections;
    private readonly string _pipeName;
    private readonly IReadOnlyDictionary<ushort, IpcEndpoint> _invokers;
    private readonly IIpcClientsVerifier _ipcClientsVerifier;

    public PipeIpcServer(int maxConCurrentConnections, string pipeName, IEnumerable<IpcEndpoint> invokers, IIpcClientsVerifier ipcClientsVerifier)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConCurrentConnections, 1);

        _maxConCurrentConnections = maxConCurrentConnections;
        _pipeName = pipeName;
        _ipcClientsVerifier = ipcClientsVerifier;
        _invokers = invokers.ToDictionary(static i => i.MessageType);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_maxConCurrentConnections == 1)
            return WorkerAsync(stoppingToken);

        var workers = new Task[_maxConCurrentConnections];

        for (int i = 0; i < workers.Length; i++)
        {
            workers[i] = WorkerAsync(stoppingToken);
        }

        return Task.WhenAll(workers);
    }

    private async Task WorkerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var pipe = CreatePipe();

            try
            {
                Debug.WriteLine("Waiting for named pipe client......");
                await pipe.WaitForConnectionAsync(stoppingToken);
                Debug.WriteLine("Named pipe client connected.");

                await ProcessRequestAsync(pipe, stoppingToken);
            }
            catch (IOException ioException) when (ioException.IsNamedPipeClientDisconnect())
            {
                Debug.WriteLine("IPC client disconnected before the server response was delivered.");
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException or EndOfStreamException)
            {
                //
            }
            catch (Exception e)
            {
                UnhandledExceptionLogger.WriteException(e);
            }
        }
    }

    private NamedPipeServerStream CreatePipe() => new(
        _pipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: _maxConCurrentConnections,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        inBufferSize: 4 * 1024,
        outBufferSize: 8 * 1024);

    private async Task ProcessRequestAsync(
        NamedPipeServerStream pipe,
        CancellationToken stoppingToken)
    {
        var authenticationLevel = _ipcClientsVerifier.IsExpectedClient(pipe);

        if (authenticationLevel == IpcAuthenticationLevel.Rejected)
        {
            Debug.WriteLine("Rejected unauthorized IPC pipe client.");
            return;
        }

        var header = await RequestHeader.ReadAsync(pipe);
        if (!_invokers.TryGetValue(header.MessageType, out var endpoint))
        {
            Debug.WriteLine($"Rejected unknown IPC message type: {header.MessageType}.");
            return;
        }

        /*if (authenticationLevel != endpoint.AuthenticationLevel)
        {
            Debug.WriteLine("Rejected IPC message with incorrect authentication level.");
            return;
        }*/

        await endpoint.Delegate.Invoke(pipe, header.PayloadLength, stoppingToken);
        await pipe.FlushAsync(stoppingToken);
    }
}
