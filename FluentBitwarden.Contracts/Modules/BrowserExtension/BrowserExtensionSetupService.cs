using System.Text.Json;
using Windows.Storage;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using Microsoft.Win32;

namespace FluentBitwarden.Contracts.Modules.BrowserExtension;

public static class BrowserExtensionSetupService
{
    private sealed record BrowserNativeHostRegistration(
        string BrowserName,
        string RegistrySubKey,
        string ManifestPath);

    public const string NativeHostName = "com.fluentbitwarden.browserhost";
    public const string ChromiumExtensionId = "gfhbajeljalknneenddbfijidlaggipo";
    public const string FirefoxExtensionId = "browser-extension@fluentbitwarden.local";

    private const string ChromeRegistrySubKey =
        @"Software\Google\Chrome\NativeMessagingHosts\" + NativeHostName;

    private const string EdgeRegistrySubKey =
        @"Software\Microsoft\Edge\NativeMessagingHosts\" + NativeHostName;

    private const string FirefoxRegistrySubKey =
        @"Software\Mozilla\NativeMessagingHosts\" + NativeHostName;

    private static readonly string[] GeneratedManifestFileNames =
    [
        "chrome.windows.json",
        "edge.windows.json",
        "firefox.windows.json",
    ];

    private static readonly string BrowserHostPath = Path.Combine(
        PackageHelper.AppBasePath,
        "FluentBitwarden.BrowserHost",
        "FluentBitwarden.BrowserHost.exe");

    private static readonly string ManifestDirectory = Path.Combine(
        ApplicationData.Current.LocalFolder.Path,
        "BrowserExtension");

    private static string GetManifestPath(string fileName) => Path.Combine(ManifestDirectory, fileName);

    public static void EnsureRegistered()
    {
        if (!File.Exists(BrowserHostPath))
        {
            throw new FileNotFoundException(
                "FluentBitwarden.BrowserHost.exe was not found in the installed package.",
                BrowserHostPath);
        }

        Directory.CreateDirectory(ManifestDirectory);

        string chromeManifestPath = GetManifestPath("chrome.windows.json");
        string edgeManifestPath = GetManifestPath("edge.windows.json");
        string firefoxManifestPath = GetManifestPath("firefox.windows.json");
        BrowserNativeHostRegistration[] registrations =
        [
            new("Chrome", ChromeRegistrySubKey, chromeManifestPath),
            new("Microsoft Edge", EdgeRegistrySubKey, edgeManifestPath),
            new("Firefox", FirefoxRegistrySubKey, firefoxManifestPath),
        ];

        WriteChromiumManifest(chromeManifestPath, BrowserHostPath);
        WriteChromiumManifest(edgeManifestPath, BrowserHostPath);
        WriteFirefoxManifest(firefoxManifestPath, BrowserHostPath);

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

    private static void WriteChromiumManifest(string manifestPath, string browserHostPath)
    {
        using FileStream stream = File.Create(manifestPath, 1024);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("name", NativeHostName);
        writer.WriteString("description", "FluentBitwarden Browser Host");
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
        writer.WriteString("description", "FluentBitwarden Browser Host");
        writer.WriteString("path", browserHostPath);
        writer.WriteString("type", "stdio");
        writer.WriteStartArray("allowed_extensions");
        writer.WriteStringValue(FirefoxExtensionId);
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.Flush();
    }
}
