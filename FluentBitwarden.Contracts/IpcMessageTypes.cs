namespace FluentBitwarden.Contracts;

public static class IpcMessageTypes
{
    public static class System
    {
        public const ushort Ping = 1;
    }

    public static class Passkey
    {
        // Keep this stable if the native COM server already uses message type 2.
        public const ushort GetAssertion = 2;
    }

    public static class Vault
    {
        public const ushort Sync = 100;
        public const ushort SearchCiphers = 101;
        public const ushort GetCipher = 102;
        public const ushort GetFolders = 103;
        public const ushort GetCollections = 104;
        public const ushort GetStatus = 105;
    }

    public static class Account
    {
        public const ushort GetUnlockedAccount = 200;
        public const ushort LogIn = 201;
        public const ushort GetAccounts = 202;
        public const ushort Unlock = 203;
        public const ushort Lock = 204;
        public const ushort Logout = 205;
        public const ushort GetActiveSession = 206;
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
        public const ushort ShowPasskeyOverlay = 400;
        public const ushort ShowSshUserActionPrompt = 401;
    }

    public static class SshAgent
    {
        public const ushort GetAvailableKeys = 500;
        public const ushort Sign = 501;
    }
}