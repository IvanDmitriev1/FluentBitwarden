namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeMessageInvoker
{ 
    ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken);
}