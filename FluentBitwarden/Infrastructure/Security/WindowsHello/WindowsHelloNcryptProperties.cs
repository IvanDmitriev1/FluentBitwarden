using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Windows.Win32;

namespace FluentBitwarden.Infrastructure.Security.WindowsHello;

internal static class WindowsHelloNcryptProperties
{
    private const string WindowHandleProperty = "HWND Handle";
    private const string UseContextProperty = "Use Context";
    private const string LengthProperty = "Length";
    private const string ExportPolicyProperty = "Export Policy";
    private const string KeyUsageProperty = "Key Usage";
    private const string NgcCacheTypeProperty = "NgcCacheType";
    private const string NgcCacheTypePropertyDeprecated = "NgcCacheTypeProperty";
    private const string PinCacheIsGestureRequiredProperty = "PinCacheIsGestureRequired";

    private const string DefaultUseContext = "Unlock your FluentBitwarden vault";
    private const int RsaKeySizeBits = 2048;
    private const int AllowDecryptFlag = 0x00000001;
    private const int NgcCacheAuthMandatoryFlag = 0x00000001;

    /// <summary>
    /// Applies size, usage, export, cache, and UI properties required for a new Windows Hello wrapping key.
    /// </summary>
    public static void ConfigureNewWrappingKey(this NCryptFreeObjectSafeHandle key, IntPtr ownerWindowHandle)
    {
        SetDwordProperty(key, LengthProperty, RsaKeySizeBits);
        SetDwordProperty(key, KeyUsageProperty, AllowDecryptFlag);
        SetDwordProperty(key, ExportPolicyProperty, 0);
        SetNgcCacheType(key);
        ApplyUiContext(key, ownerWindowHandle);
    }

    /// <summary>
    /// Marks the next private-key use as requiring a fresh Windows Hello gesture.
    /// </summary>
    public static void RequireGestureOnNextUse(this NCryptFreeObjectSafeHandle key)
        => SetDwordProperty(key, PinCacheIsGestureRequiredProperty, 1);

    /// <summary>
    /// Attaches the owner window and prompt context used by the Windows Hello UI.
    /// </summary>
    public static void ApplyUiContext(this NCryptFreeObjectSafeHandle key, IntPtr ownerWindowHandle)
    {
        ApplyWindowHandle(key, ownerWindowHandle);
        SetStringProperty(key, UseContextProperty, DefaultUseContext);
    }

    /// <summary>
    /// Associates NCrypt UI with the app window so Windows Hello prompts are parented correctly.
    /// </summary>
    public static void ApplyWindowHandle(this NCryptFreeObjectSafeHandle key, IntPtr ownerWindowHandle)
    {
        Span<byte> handleBytes = stackalloc byte[IntPtr.Size];
        if (IntPtr.Size == sizeof(long))
            BinaryPrimitives.WriteInt64LittleEndian(handleBytes, ownerWindowHandle.ToInt64());
        else
            BinaryPrimitives.WriteInt32LittleEndian(handleBytes, ownerWindowHandle.ToInt32());

        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptSetProperty(key, WindowHandleProperty, handleBytes, 0),
            WindowHandleProperty,
            ignoredStatus: WindowsHelloNcryptStatus.NteBadData);
    }

    /// <summary>
    /// Configures Passport cache behavior so private-key use requires Windows Hello authentication.
    /// </summary>
    private static void SetNgcCacheType(this NCryptFreeObjectSafeHandle key)
    {
        try
        {
            SetDwordProperty(key, NgcCacheTypeProperty, NgcCacheAuthMandatoryFlag);
        }
        catch (CryptographicException)
        {
            SetDwordProperty(key, NgcCacheTypePropertyDeprecated, NgcCacheAuthMandatoryFlag);
        }
    }

    /// <summary>
    /// Writes an integer NCrypt property in the format expected by the Passport provider.
    /// </summary>
    private static void SetDwordProperty(this NCryptFreeObjectSafeHandle key, string propertyName, int value)
    {
        Span<byte> valueBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(valueBytes, value);

        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptSetProperty(key, propertyName, valueBytes, 0),
            propertyName);
    }

    /// <summary>
    /// Writes a null-terminated UTF-16 NCrypt property value.
    /// </summary>
    private static void SetStringProperty(this NCryptFreeObjectSafeHandle key, string propertyName, string value)
    {
        byte[] valueBytes = Encoding.Unicode.GetBytes(value + '\0');

        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptSetProperty(key, propertyName, valueBytes, 0),
            propertyName);
    }
}
