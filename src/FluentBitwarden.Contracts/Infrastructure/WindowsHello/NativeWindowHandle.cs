namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello;

[MemoryPackable]
public readonly partial record struct NativeWindowHandle(long Value)
{
    public bool IsEmpty => Value == 0;
}
