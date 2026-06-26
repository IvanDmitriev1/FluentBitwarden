using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcServer(
    string pipeName,
    IReadOnlyDictionary<ushort, IpcRpcEndpoint> endpoints,
    IIpcClientsVerifier ipcClientsVerifier)
    : BackgroundService
{
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

                _ = ProcessRequestAsync(requestPipe, stoppingToken);
            }
            catch (IOException)
            {
                Debug.WriteLine("IPC client disconnected before the server response was delivered.");
            }
            catch (OperationCanceledException)
            {
                //
            }
            catch (Exception e)
            {
                UnhandledExceptionLogger.WriteException(e);
            }
            finally
            {
                pipe?.Dispose();
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

    private async Task ProcessRequestAsync(
        NamedPipeServerStream pipe,
        CancellationToken stoppingToken)
    {
        try
        {
            var authenticationLevel = ipcClientsVerifier.IsExpectedClient(pipe);
            if (authenticationLevel == IpcAuthenticationLevel.Rejected)
            {
                Debug.WriteLine("Rejected unauthorized IPC pipe client.");
                return;
            }

            var header = await IpcMessageHeader.ReadAsync(pipe, stoppingToken);
            if (!endpoints.TryGetValue(header.MessageType, out var endpoint))
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
        catch (IOException)
        {
            Debug.WriteLine("IPC client disconnected before the server response was delivered.");
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
        }
        finally
        {
            pipe.Dispose();
        }
    }
}
