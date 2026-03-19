using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

public enum DeviceType
{
    Android = 0,
    iOS = 1,
    ChromeExtension = 2,
    FirefoxExtension = 3,
    OperaExtension = 4,
    EdgeExtension = 5,
    WindowsDesktop = 6,
    MacOsDesktop = 7,
    LinuxDesktop = 8,
    ChromeBrowser = 9,
    FirefoxBrowser = 10,
    OperaBrowser = 11,
    EdgeBrowser = 12,
    IeBrowser = 13,
    UnknownBrowser = 14,
    AndroidAmazon = 15,
    Uwp = 16,
    SafariBrowser = 17,
    VivaldiBrowser = 18,
    VivaldiExtension = 19,
    SafariExtension = 20,
    Sdk = 21,
    Server = 22,
    WindowsCli = 23,
    MacOsCli = 24,
    LinuxCli = 25,
}

public sealed record DeviceInfo(
    DeviceIdentifier DeviceIdentifier,
    DeviceName DeviceName,
    DeviceType DeviceType);
