using System.Diagnostics.CodeAnalysis;
using AsyncAwaitBestPractices;
using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcServer(
    string pipeName,
    IReadOnlyDictionary<ushort, IpcRpcEndpoint> endpoints,
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
    private async Task ProcessRequestAsync(
        NamedPipeServerStream pipe,
        CancellationToken stoppingToken)
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

            await endpoint.Delegate.Invoke(pipe, header.PayloadLength, stoppingToken);
            await pipe.FlushAsync(stoppingToken);
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
}
