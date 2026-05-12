using BitwardenApi.Models;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace FluentBitwarden.Infrastructure.Security;

public static class DeviceIdentity
{
    private static readonly DeviceName DeviceName = new DeviceName($"{Environment.MachineName} FluentBitwarden");
    private static DeviceIdentifier DeviceId => DeviceIdLazy.Value;
    private static readonly Lazy<DeviceIdentifier> DeviceIdLazy = new(ComputeDeviceId);


    public static readonly DeviceInfo DeviceInfo = new(DeviceId, DeviceName);


    private static DeviceIdentifier ComputeDeviceId()
    {
        string?[] components =
        [
            GetMachineGuid(),
            Environment.MachineName
        ];

        var raw = string.Join("|", components);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var guid = new Guid(hash[..16]);
        return new DeviceIdentifier(guid.ToString());
    }

    private static string? GetMachineGuid()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return key?.GetValue("MachineGuid") as string;
    }
}