namespace FluentBitwarden.Shared.Behaviors.PageLyfecycle;

internal readonly record struct PageLoadInvocation(ulong Generation, CancellationToken CancellationToken);
