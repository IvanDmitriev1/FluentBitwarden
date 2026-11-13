using Microsoft.Windows.SDK.BuildTools.WinApp.UIAutomation;

namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

internal static class UiSelectors
{
    public static UiSelector ById(string id, string? root = null) => new()
    {
        Query = id,
        Root = root is null ? null : new UiSelector { Query = root }
    };
}
