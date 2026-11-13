namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DesktopUiCollection : ICollectionFixture<DesktopUiFixture>
{
    public const string Name = "Desktop UI";
}
