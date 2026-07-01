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
        public const ushort GetFolders = 103;
        public const ushort GetCollections = 104;
        public const ushort GetStatus = 105;
        public const ushort SessionStatusChanged = 106;
    }

    public static class Account
    {
        public const ushort LogIn = 201;
        public const ushort GetAccounts = 202;
        public const ushort Unlock = 203;
        public const ushort Logout = 205;
        public const ushort GetUnlocked = 206;
        public const ushort GetUnlockedProfileDetails = 207;
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
