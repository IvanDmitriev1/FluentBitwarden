namespace FluentBitwarden.Contracts.Modules;

public static class IpcMessageTypes
{
    public static class System
    {
        public const ushort Ping = 1;
    }

    public static class Passkey
    {
        public const ushort GetAssertion = 50;
    }

    public static class Vault
    {
        public const ushort Sync = 100;
        public const ushort SearchCiphers = 101;
        public const ushort GetCipher = 102;
        public const ushort SessionStatusChanged = 106;
        public const ushort DownloadCipherAttachment = 107;
        public const ushort SaveCipher = 108;
    }

    public static class Account
    {
        public const ushort LogIn = 201;
        public const ushort GetAccounts = 202;
        public const ushort Unlock = 203;
    }

    public static class Session
    {
        public const ushort GetUnlockedAccount = 600;
        public const ushort Lock = 601;
        public const ushort GetStatus = 602;
    }

    public static class WindowsHello
    {
        public const ushort GetCurrentAccountStatus = 300;
        public const ushort GetAccountStatus = 301;
        public const ushort Enable = 302;
        public const ushort Disable = 303;
    }

    public static class Ui
    {
        public const ushort ShowUnlockDialog = 400;
        public const ushort ShowSshDialog = 401;
    }

    public static class Browser
    {
        public const ushort GetVaultStatus = 500;
        public const ushort GetCredentialAvailability = 501;
        public const ushort GetCredentialFill = 502;
    }
}
