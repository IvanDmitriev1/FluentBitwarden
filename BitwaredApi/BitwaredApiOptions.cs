namespace BitwaredApi;

public sealed class BitwaredApiOptions
{
    public BitwardenEnvironment Environment { get; set; } = BitwardenEnvironment.UnitedStates;

    public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

    public Action<string, Exception?>? DiagnosticSink { get; set; }

    public Abstractions.IClock? ClockOverride { get; set; }

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Environment);
        ArgumentNullException.ThrowIfNull(Environment.ApiBase);
        ArgumentNullException.ThrowIfNull(Environment.IdentityBase);
    }
}
