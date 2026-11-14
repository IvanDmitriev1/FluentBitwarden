using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using FluentBitwarden.Ui.Features.Settings.Models;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Settings;

[Trait("Category", "Unit")]
public sealed class SettingValueTests
{
    [Fact]
    public void LoadsStoredValueEditsPersistsReloadsWithoutWritingAndResetsToDefault()
    {
        var store = Substitute.For<ISettingsStore>();
        var key = new SettingKey<bool>("test.setting", true);
        bool persisted = false;
        store.Get(key).Returns(_ => persisted);
        store.Set(key, Arg.Do<bool>(value => persisted = value));

        var setting = new SettingValue<bool>(store, key);
        Assert.False(setting.Value);
        store.DidNotReceive().Set(key, Arg.Any<bool>());

        setting.Value = true;
        Assert.True(persisted);
        store.Received(1).Set(key, true);

        persisted = false;
        store.ClearReceivedCalls();
        setting.Reload();
        Assert.False(setting.Value);
        store.DidNotReceive().Set(key, Arg.Any<bool>());

        store.ClearReceivedCalls();
        setting.Reset();
        Assert.True(setting.Value);
        Assert.True(persisted);
        store.Received(1).Set(key, true);
    }
}
