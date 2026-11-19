using FluentBitwarden.Platform.Infrastructure.Clipboard;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FluentBitwarden.Platform.Tests.Clipboard;

public sealed class ClipboardManagerRegistrationTests
{
    [Fact]
    public void AddPlatformServicesRegistersClipboardManagerAsSingleton()
    {
        using var provider = new ServiceCollection()
            .AddPlatformServices()
            .BuildServiceProvider();

        var first = provider.GetRequiredService<IClipboardManager>();

        Assert.Same(first, provider.GetRequiredService<IClipboardManager>());
    }

    [Fact]
    public void AddPlatformServicesPreservesExistingClipboardManager()
    {
        var expected = Substitute.For<IClipboardManager>();
        var services = new ServiceCollection();
        services.AddSingleton<IClipboardManager>(expected);
        services.AddPlatformServices();

        using var provider = services.BuildServiceProvider();

        Assert.Same(expected, provider.GetRequiredService<IClipboardManager>());
    }
}
