using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastracture.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastracture.Hosting;

public sealed class WinUiHostApplicationBuilder<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication>
    : IHostApplicationBuilder
    where TApplication : Application
{
    private readonly HostApplicationBuilder _builder;
    private string? _instanceKey;

    internal WinUiHostApplicationBuilder()
    {
        _builder = Host.CreateApplicationBuilder();
    }

    public IConfigurationManager Configuration =>
        ((IHostApplicationBuilder)_builder).Configuration;

    public IHostEnvironment Environment =>
        _builder.Environment;

    public IServiceCollection Services =>
        _builder.Services;

    public ILoggingBuilder Logging =>
        _builder.Logging;

    public IMetricsBuilder Metrics =>
        _builder.Metrics;

    public IDictionary<object, object> Properties =>
        ((IHostApplicationBuilder)_builder).Properties;

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull =>
        _builder.ConfigureContainer(factory, configure);


    public WinUiHostApplicationBuilder<TApplication> UseSingleInstance(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _instanceKey = key;
        return this;
    }

    public IWinUiHost Build()
    {
        Services.AddHostedService<WinUiHostedService<TApplication>>();

        IHost host = _builder.Build();

        if (!string.IsNullOrWhiteSpace(_instanceKey))
        {
            return new SingleInstanceWinUiHost<TApplication>(host, _instanceKey);
        }

        return new WinUiHost(host);
    }
}
