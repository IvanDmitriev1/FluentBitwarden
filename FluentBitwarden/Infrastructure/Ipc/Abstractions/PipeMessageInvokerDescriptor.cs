namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

internal sealed record PipeMessageInvokerDescriptor(
    ushort MessageType,
    Func<IServiceProvider, IPipeMessageInvoker> CreateInvoker);