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

    private const int MaxConnections = 16;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var slots = new SemaphoreSlim(MaxConnections);
        bool firstInstance = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await slots.WaitAsync(stoppingToken);

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
                    DefaultPipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    options,
                    inBufferSize: 1 * 1024,
                    outBufferSize: 8 * 1024);

                try
                {
                    await server.WaitForConnectionAsync(stoppingToken);
                }
                catch (IOException e) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.AgentAcceptFailed(e);
                    continue;
                }

                firstInstance = false;
                _ = HandleConnectionAsync(server, slots, stoppingToken);
                server = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            finally
            {
                server?.Dispose();
                slots.Release();
            }
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        SemaphoreSlim slots,
        CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var messagesHandler = scope.ServiceProvider.GetRequiredService<ISshAgentMessagesHandler>();

        try
        {
            await using NamedPipeServerStream _ = pipe;

            while (!stoppingToken.IsCancellationRequested)
            {
                using var idleReadCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                idleReadCts.CancelAfter(ClientIdleTimeout);

                try
                {
                    var message = await SshAgentMessage.ReadAsync(pipe, idleReadCts.Token);

                    if (message == SshAgentMessage.Failure)
                        return;

                    var response = await messagesHandler.HandleAsync(message, stoppingToken);
                    await response.WriteMessageAsync(pipe, stoppingToken);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
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
        finally
        {
            slots.Release();
        }
    }
}
