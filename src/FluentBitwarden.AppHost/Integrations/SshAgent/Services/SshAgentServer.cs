using System.IO.Pipes;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshAgentServer(
    IServiceScopeFactory scopeFactory,
    ILogger<SshAgentServer> logger)
    : BackgroundService
{
    private const string DefaultPipeName = "openssh-ssh-agent";
    private static readonly TimeSpan ClientIdleTimeout = TimeSpan.FromMinutes(1);

    private const int MaxConnections = 4;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => RunAsync(DefaultPipeName, stoppingToken);

    internal async Task RunAsync(string pipeName, CancellationToken stoppingToken)
    {
        try
        {
            await Parallel.ForEachAsync(
                AcceptConnectionsAsync(pipeName, stoppingToken),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxConnections,
                    CancellationToken = stoppingToken
                },
                HandleConnectionAsync);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async IAsyncEnumerable<NamedPipeServerStream> AcceptConnectionsAsync(
        string pipeName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool firstInstance = true;

        while (true)
        {
            NamedPipeServerStream? server = null;

            try
            {
                var options =
                    PipeOptions.Asynchronous |
                    PipeOptions.CurrentUserOnly;

                if (firstInstance)
                {
                    options |= PipeOptions.FirstPipeInstance;
                }

                server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    options,
                    inBufferSize: 1 * 1024,
                    outBufferSize: 8 * 1024);

                await server.WaitForConnectionAsync(cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
            catch (IOException e) when (server is not null && !cancellationToken.IsCancellationRequested)
            {
                server.Dispose();
                logger.AgentAcceptFailed(e);
                continue;
            }
            catch
            {
                server?.Dispose();
                throw;
            }

            firstInstance = false;
            NamedPipeServerStream acceptedServer = server;
            yield return acceptedServer;
        }
    }

    private async ValueTask HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        try
        {
            await using NamedPipeServerStream _ = pipe;

            while (!stoppingToken.IsCancellationRequested)
            {
                using var idleReadCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                CancellationToken cancellationToken = idleReadCts.Token;
                idleReadCts.CancelAfter(ClientIdleTimeout);

                try
                {
                    var message = await SshAgentMessage.ReadAsync(pipe, cancellationToken);

                    if (message == SshAgentMessage.Failure)
                        return;

                    await using var scope = scopeFactory.CreateAsyncScope();
                    var messagesHandler = scope.ServiceProvider.GetRequiredService<ISshAgentMessagesHandler>();

                    var response = await messagesHandler.HandleAsync(message, cancellationToken);
                    await response.WriteMessageAsync(pipe, cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (EndOfStreamException)
        {
            // Client disconnected during a message.
        }
        catch (IOException)
        {
            // Broken pipe or invalid framing.
            // Only this connection is terminated.
        }
        catch (Exception e)
        {
            logger.AgentLoopFailed(e);
        }
    }
}
