using System.Text.Json;
using Windows.Storage;
using Microsoft.Win32;
using FluentBitwarden.Contracts.Extensions;

namespace FluentBitwarden.Contracts.Modules.BrowserExtension;

public static class BrowserExtensionSetupService
{
    private const string LegacyNativeHostName = "com.fluentbitwarden." + "browserhost";
    private const string BrowseProxyDescription = "FluentBitwarden Browse Proxy";

    private sealed record BrowserNativeHostRegistration(
        string BrowserName,
        string RegistrySubKey,
        string ManifestPath);

    public const string NativeHostName = "com.fluentbitwarden.browseproxy";
    public const string ChromiumExtensionId = "aopmhembdchhafellalkhdjkcepaeake";
    public const string FirefoxExtensionId = "browser-extension@fluentbitwarden.local";

    private static readonly string ChromeRegistrySubKey = BuildChromiumRegistrySubKey("Google\\Chrome");
    private static readonly string EdgeRegistrySubKey = BuildChromiumRegistrySubKey("Microsoft\\Edge");
    private static readonly string FirefoxRegistrySubKey = BuildFirefoxRegistrySubKey(NativeHostName);
    private static readonly string LegacyChromeRegistrySubKey = BuildLegacyChromiumRegistrySubKey("Google\\Chrome");
    private static readonly string LegacyEdgeRegistrySubKey = BuildLegacyChromiumRegistrySubKey("Microsoft\\Edge");
    private static readonly string LegacyFirefoxRegistrySubKey = BuildFirefoxRegistrySubKey(LegacyNativeHostName);

    private static readonly string[] GeneratedManifestFileNames =
    [
        "chrome.windows.json",
        "edge.windows.json",
        "firefox.windows.json",
    ];

    private static readonly string BrowseProxyPath = Path.Combine(
        PackageHelper.AppBasePath,
        "FluentBitwarden.BrowseProxy",
        "FluentBitwarden.BrowseProxy.exe");

    private static readonly string ManifestDirectory = Path.Combine(
        ApplicationData.Current.LocalFolder.Path,
        "BrowserExtension");

    private static string GetManifestPath(string fileName) => Path.Combine(ManifestDirectory, fileName);

    public static void EnsureRegistered()
    {
        if (!File.Exists(BrowseProxyPath))
        {
            throw new FileNotFoundException(
                "FluentBitwarden.BrowseProxy.exe was not found in the installed package.",
                BrowseProxyPath);
        }

        Directory.CreateDirectory(ManifestDirectory);
        DeleteLegacyRegistrations();

        string chromeManifestPath = GetManifestPath("chrome.windows.json");
        string edgeManifestPath = GetManifestPath("edge.windows.json");
        string firefoxManifestPath = GetManifestPath("firefox.windows.json");
        BrowserNativeHostRegistration[] registrations =
        [
            new("Chrome", ChromeRegistrySubKey, chromeManifestPath),
            new("Microsoft Edge", EdgeRegistrySubKey, edgeManifestPath),
            new("Firefox", FirefoxRegistrySubKey, firefoxManifestPath),
        ];

        WriteChromiumManifest(chromeManifestPath, BrowseProxyPath);
        WriteChromiumManifest(edgeManifestPath, BrowseProxyPath);
        WriteFirefoxManifest(firefoxManifestPath, BrowseProxyPath);

        foreach (BrowserNativeHostRegistration registration in registrations)
        {
            RegisterNativeHost(registration);
        }
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree(ChromeRegistrySubKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(EdgeRegistrySubKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(FirefoxRegistrySubKey, throwOnMissingSubKey: false);
        DeleteLegacyRegistrations();

        foreach (var fileName in GeneratedManifestFileNames)
        {
            File.Delete(GetManifestPath(fileName));
        }
    }

    private static void RegisterNativeHost(BrowserNativeHostRegistration registration)
    {
        using var key = Registry.CurrentUser.CreateSubKey(registration.RegistrySubKey, writable: true) ??
                        throw new InvalidOperationException($"Could not create {registration.BrowserName} native messaging registry key.");

        key.SetValue(string.Empty, registration.ManifestPath, RegistryValueKind.String);
    }

    private static void DeleteLegacyRegistrations()
    {
        Registry.CurrentUser.DeleteSubKeyTree(LegacyChromeRegistrySubKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(LegacyEdgeRegistrySubKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(LegacyFirefoxRegistrySubKey, throwOnMissingSubKey: false);
    }

    private static string BuildChromiumRegistrySubKey(string browserPath) =>
        $@"Software\{browserPath}\NativeMessagingHosts\{NativeHostName}";

    private static string BuildLegacyChromiumRegistrySubKey(string browserPath) =>
        $@"Software\{browserPath}\NativeMessagingHosts\{LegacyNativeHostName}";

    private static string BuildFirefoxRegistrySubKey(string hostName) =>
        $@"Software\Mozilla\NativeMessagingHosts\{hostName}";

    private static void WriteChromiumManifest(string manifestPath, string browserHostPath)
    {
        using FileStream stream = File.Create(manifestPath, 1024);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("name", NativeHostName);
        writer.WriteString("description", BrowseProxyDescription);
        writer.WriteString("path", browserHostPath);
        writer.WriteString("type", "stdio");
        writer.WriteStartArray("allowed_origins");
        writer.WriteStringValue($"chrome-extension://{ChromiumExtensionId}/");
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.Flush();
    }

    private static void WriteFirefoxManifest(string manifestPath, string browserHostPath)
    {
        using FileStream stream = File.Create(manifestPath, 1024);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("name", NativeHostName);
        writer.WriteString("description", BrowseProxyDescription);
        writer.WriteString("path", browserHostPath);
        writer.WriteString("type", "stdio");
        writer.WriteStartArray("allowed_extensions");
        writer.WriteStringValue(FirefoxExtensionId);
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.Flush();
    }
}
