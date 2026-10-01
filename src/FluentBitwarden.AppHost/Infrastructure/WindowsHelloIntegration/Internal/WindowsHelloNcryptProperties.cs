using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration.Internal;

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

    private const int RsaKeySizeBits = 2048;
    private const int AllowDecryptFlag = 0x00000001;
    private const int NgcCacheAuthMandatoryFlag = 0x00000001;

    extension(NCryptFreeObjectSafeHandle key)
    {
        /// <summary>
        /// Applies size, usage, export, cache, and UI properties required for a new Windows Hello wrapping key.
        /// </summary>
        public void ConfigureNewWrappingKey(IntPtr ownerWindowHandle, string useContext)
        {
            key.SetDwordProperty(LengthProperty, RsaKeySizeBits);
            key.SetDwordProperty(KeyUsageProperty, AllowDecryptFlag);
            key.SetDwordProperty(ExportPolicyProperty, 0);
            key.SetNgcCacheType();
            key.ApplyUiContext(ownerWindowHandle, useContext);
        }

        /// <summary>
        /// Marks the next private-key use as requiring a fresh Windows Hello gesture.
        /// </summary>
        public void RequireGestureOnNextUse()
            => key.SetDwordProperty(PinCacheIsGestureRequiredProperty, 1);

        /// <summary>
        /// Attaches the owner window and prompt context used by the Windows Hello UI.
        /// </summary>
        public void ApplyUiContext(IntPtr ownerWindowHandle, string useContext)
        {
            key.ApplyWindowHandle(ownerWindowHandle);
            key.SetStringProperty(UseContextProperty, useContext);
        }

        /// <summary>
        /// Associates NCrypt UI with the app window so Windows Hello prompts are parented correctly.
        /// </summary>
        public void ApplyWindowHandle(IntPtr ownerWindowHandle)
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
        private void SetNgcCacheType()
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
        private void SetDwordProperty(string propertyName, int value)
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
        private void SetStringProperty(string propertyName, string value)
        {
            byte[] valueBytes = Encoding.Unicode.GetBytes(value + '\0');

            WindowsHelloNcryptStatus.ThrowIfFailed(
                PInvoke.NCryptSetProperty(key, propertyName, valueBytes, 0),
                propertyName);
        }
    }
}
