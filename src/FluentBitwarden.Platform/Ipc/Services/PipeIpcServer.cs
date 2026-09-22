using AsyncAwaitBestPractices;
using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcServer(
    string pipeName,
    IReadOnlyDictionary<ushort, IpcRpcEndpoint> endpoints,
    IServiceScopeFactory scopeFactory,
    IIpcClientsVerifier ipcClientsVerifier,
    ILogger<PipeIpcServer> logger)
    : BackgroundService
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; narrowing would break resilience against unanticipated transport failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = CreatePipe();

            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken);

                var requestPipe = pipe;
                pipe = null;

                ProcessRequestAsync(requestPipe, stoppingToken).SafeFireAndForget();
            }
            catch (IOException)
            {
                logger.ClientDisconnected();
            }
            catch (OperationCanceledException)
            {
                //
            }
            catch (Exception e)
            {
                logger.ServerLoopFailed(e);
            }
            finally
            {
                if (pipe is not null)
                    await pipe.DisposeAsync();
            }
        }
    }

    private NamedPipeServerStream CreatePipe() => new(
        pipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        inBufferSize: 4 * 1024,
        outBufferSize: 8 * 1024);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; narrowing would break resilience against unanticipated transport failures.")]
    private async Task ProcessRequestAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        try
        {
            var authenticationLevel = ipcClientsVerifier.IsExpectedClient(pipe);
            if (authenticationLevel == IpcAuthenticationLevel.Rejected)
            {
                logger.UnauthorizedClientRejected();
                return;
            }

            var header = await IpcMessageHeader.ReadAsync(pipe, stoppingToken);
            if (!endpoints.TryGetValue(header.MessageType, out var endpoint))
            {
                logger.UnknownMessageTypeRejected(header.MessageType);
                return;
            }

            /*if (authenticationLevel != endpoint.AuthenticationLevel)
            {
                Debug.WriteLine("Rejected IPC message with incorrect authentication level.");
                return;
            }*/

            byte[] payload = new byte[header.PayloadLength];
            await pipe.ReadExactlyAsync(payload, 0, payload.Length, stoppingToken);

            IpcRpcInvocationResult result;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                result = await endpoint.InvokeAsync(scope.ServiceProvider, pipe, payload, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                result = IpcRpcInvocationResult.Failure(exception);
            }

            switch (result.Status)
            {
                case IpcRpcInvocationStatus.NoResponse:
                    return;

                case IpcRpcInvocationStatus.Failure:
                    logger.RequestFailed(result.Exception!);
                    await TryWriteFailureResponseAsync(pipe, stoppingToken);
                    return;

                case IpcRpcInvocationStatus.Success:
                    if (stoppingToken.IsCancellationRequested)
                        return;

                    await TryWriteSuccessResponseAsync(pipe, result.ResponsePayload!, stoppingToken);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (IOException)
        {
            logger.ClientDisconnected();
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception e)
        {
            logger.RequestFailed(e);
        }
        finally
        {
            await pipe.DisposeAsync();
        }
    }

    private async Task TryWriteSuccessResponseAsync(
        NamedPipeServerStream pipe,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await IpcWireProtocol.WriteRpcResponseAsync(pipe, payload, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            logger.ClientDisconnected();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown or a response write cancellation closes the connection.
        }
        catch (Exception exception)
        {
            logger.RequestFailed(exception);
        }
    }

    private async Task TryWriteFailureResponseAsync(
        NamedPipeServerStream pipe,
        CancellationToken cancellationToken)
    {
        try
        {
            await IpcWireProtocol.WriteRpcFailureResponseAsync(pipe, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            logger.ClientDisconnected();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown closes the connection without delivering a response.
        }
        catch (Exception exception)
        {
            logger.RequestFailed(exception);
        }
    }
}
