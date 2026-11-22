using System.Collections.Concurrent;
using System.IO.Pipes;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentServerTests
{
    [Fact]
    public async Task Forwarding_state_stays_on_its_pipe_across_real_request_scopes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"ssh-agent-test-{Guid.NewGuid():N}";
        var calls = new ConcurrentQueue<(Guid ScopeId, bool IsForwarded)>();
        using var services = new ServiceCollection()
            .AddScoped<ISshKeyProvider>(_ =>
            {
                Guid scopeId = Guid.NewGuid();
                var keyProvider = Substitute.For<ISshKeyProvider>();
                keyProvider.GetSignatureAsync(
                        Arg.Any<SshAgentSignRequest>(),
                        Arg.Any<SshAgentSessionBinding?>(),
                        Arg.Do<bool>(isForwarded => calls.Enqueue((scopeId, isForwarded))),
                        Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<SshAgentSignature?>(new SshAgentSignature("ssh-ed25519", [0x01])));
                return keyProvider;
            })
            .AddScoped<ISshAgentMessagesHandler, SshAgentMessagesHandler>()
            .BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var ioTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ioTimeout.CancelAfter(TimeSpan.FromSeconds(15));
        CancellationToken ioToken = ioTimeout.Token;
        var clients = new List<NamedPipeClientStream>();
        var serverTask = server.RunAsync(pipeName, stopping.Token);

        try
        {
            await SshAgentNamedPipeTestSupport.WaitForListenerAsync(pipeName, ioToken);
            var forwardedClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            clients.Add(forwardedClient);
            await forwardedClient.ConnectAsync(ioToken).WaitAsync(TimeSpan.FromSeconds(5), ioToken);

            await SshAgentTestData.CreateForwardingNotification()
                .WriteMessageAsync(forwardedClient, ioToken);
            SshAgentMessage forwardingResponse = await SshAgentMessage.ReadAsync(forwardedClient, ioToken);
            Assert.Equal(SshAgentMessageType.ExtensionFailure, forwardingResponse.Type);

            var signRequest = new SshAgentMessage(
                SshAgentMessageType.SignRequest,
                SshAgentTestData.EncodeSignRequest([0xAA], [0xBB], 0));
            await signRequest.WriteMessageAsync(forwardedClient, ioToken);
            Assert.Equal(SshAgentMessageType.SignResponse,
                (await SshAgentMessage.ReadAsync(forwardedClient, ioToken)).Type);

            await signRequest.WriteMessageAsync(forwardedClient, ioToken);
            Assert.Equal(SshAgentMessageType.SignResponse,
                (await SshAgentMessage.ReadAsync(forwardedClient, ioToken)).Type);

            var freshClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            clients.Add(freshClient);
            await freshClient.ConnectAsync(ioToken).WaitAsync(TimeSpan.FromSeconds(5), ioToken);
            await signRequest.WriteMessageAsync(freshClient, ioToken);
            Assert.Equal(SshAgentMessageType.SignResponse,
                (await SshAgentMessage.ReadAsync(freshClient, ioToken)).Type);

            (Guid ScopeId, bool IsForwarded)[] observedCalls = calls.ToArray();
            Assert.Equal(3, observedCalls.Length);
            Assert.True(observedCalls[0].IsForwarded);
            Assert.True(observedCalls[1].IsForwarded);
            Assert.False(observedCalls[2].IsForwarded);
            Assert.NotEqual(observedCalls[0].ScopeId, observedCalls[1].ScopeId);
            Assert.NotEqual(observedCalls[1].ScopeId, observedCalls[2].ScopeId);
        }
        finally
        {
            stopping.Cancel();
            foreach (NamedPipeClientStream client in clients)
                client.Dispose();
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await serverTask.WaitAsync(cleanup.Token);
        }
    }

    [Fact]
    public async Task Accepts_four_connections_and_waits_for_capacity_before_accepting_the_fifth()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"ssh-agent-test-{Guid.NewGuid():N}";
        var handler = Substitute.For<ISshAgentMessagesHandler>();
        using var services = new ServiceCollection()
            .AddSingleton(handler)
            .BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var pendingConnectStopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var clients = new List<NamedPipeClientStream>();
        Task? pendingConnect = null;
        var serverTask = server.RunAsync(pipeName, stopping.Token);

        NamedPipeClientStream NewClient()
        {
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            clients.Add(client);
            return client;
        }

        try
        {
            for (var index = 0; index < 4; index++)
            {
                await NewClient().ConnectAsync(cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }

            Task pendingConnectTask = NewClient().ConnectAsync(pendingConnectStopping.Token);
            pendingConnect = pendingConnectTask;
            await Assert.ThrowsAsync<TimeoutException>(
                () => pendingConnectTask.WaitAsync(TimeSpan.FromMilliseconds(100), cancellationToken));

            clients[0].Dispose();
            await pendingConnect.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        finally
        {
            stopping.Cancel();
            pendingConnectStopping.Cancel();
            foreach (NamedPipeClientStream client in clients)
            {
                client.Dispose();
            }

            if (pendingConnect is not null)
            {
                try
                {
                    await pendingConnect.WaitAsync(cleanup.Token);
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                catch (OperationCanceledException)
                {
                }
            }

            await serverTask.WaitAsync(cleanup.Token);
        }
    }

    [Fact]
    public async Task Closes_pipe_and_continues_accepting_when_handler_resolution_fails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"ssh-agent-test-{Guid.NewGuid():N}";
        using var services = new ServiceCollection().BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var serverTask = server.RunAsync(pipeName, stopping.Token);

        try
        {
            for (var index = 0; index < 17; index++)
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await client.ConnectAsync(cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                await new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty)
                    .WriteMessageAsync(client, cancellationToken);

                var buffer = new byte[1];
                var readTask = client.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                var bytesRead = -1;
                try
                {
                    bytesRead = await readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (IOException)
                {
                }

                Assert.True(bytesRead == 0 || readTask.IsFaulted);
            }
        }
        finally
        {
            stopping.Cancel();
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await serverTask.WaitAsync(cleanup.Token);
        }
    }

    [Fact]
    public async Task Waits_for_a_blocked_handler_and_its_scope_to_finish_during_shutdown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"ssh-agent-test-{Guid.NewGuid():N}";
        var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHandler = new TaskCompletionSource<SshAgentMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopeDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Substitute.For<ISshAgentMessagesHandler>();
        handler.HandleAsync(
                Arg.Any<SshAgentMessage>(),
                Arg.Any<SshAgentConnectionState>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                handlerEntered.TrySetResult();
                return releaseHandler.Task;
            });
        using var services = new ServiceCollection()
            .AddScoped(_ => new AsyncDisposableProbe(scopeDisposed))
            .AddScoped<ISshAgentMessagesHandler>(provider =>
            {
                _ = provider.GetRequiredService<AsyncDisposableProbe>();
                return handler;
            })
            .BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        var serverTask = server.RunAsync(pipeName, stopping.Token);

        try
        {
            await client.ConnectAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            await new SshAgentMessage(SshAgentMessageType.RequestIdentities, ReadOnlyMemory<byte>.Empty)
                .WriteMessageAsync(client, cancellationToken);
            await handlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            stopping.Cancel();
            await Assert.ThrowsAsync<TimeoutException>(
                () => serverTask.WaitAsync(TimeSpan.FromMilliseconds(200), cancellationToken));

            releaseHandler.TrySetResult(SshAgentMessage.Failure);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            await scopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        finally
        {
            stopping.Cancel();
            releaseHandler.TrySetResult(SshAgentMessage.Failure);
            client.Dispose();
            await serverTask.WaitAsync(cleanup.Token);
            await scopeDisposed.Task.WaitAsync(cleanup.Token);
        }
    }

    [Fact]
    public async Task Cancels_acceptance_when_stopping_before_any_client_connects()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"ssh-agent-test-{Guid.NewGuid():N}";
        using var services = new ServiceCollection().BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var serverTask = server.RunAsync(pipeName, stopping.Token);

        try
        {
            await SshAgentNamedPipeTestSupport.WaitForListenerAsync(pipeName, cancellationToken);
        }
        finally
        {
            stopping.Cancel();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    [Fact]
    public async Task Propagates_pipe_creation_errors()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var services = new ServiceCollection().BuildServiceProvider();
        var server = new SshAgentServer(
            services.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<SshAgentServer>>());

        await Assert.ThrowsAsync<ArgumentException>(() => server.RunAsync(string.Empty, cancellationToken));
    }

    private sealed class AsyncDisposableProbe(TaskCompletionSource disposed) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            disposed.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

}
