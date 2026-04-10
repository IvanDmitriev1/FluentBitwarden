using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security;

namespace FluentBitwarden.Modules.Session.Services
{
    internal sealed class CurrentSessionAccessor : ICurrentSessionAccessor
    {
        public bool IsAuthenticated { get; private set; }

        public BitwardenEnvironment CurrentEnvironment => CurrentContext.Environment;

        public BitwardenClientContext CurrentContext
        {
            get => field == default
                ? throw new InvalidOperationException()
                : field;
            private set;
        }

        [field: MaybeNull]
        public UserId CurrentUser
        {
            get => field == UserId.Empty
                ? throw new InvalidOperationException()
                : field;
            private set;
        }

        [field: MaybeNull]
        public SessionTokens CurrentSession
        {
            get => field ?? throw new InvalidOperationException();
            private set;
        }

        [field: MaybeNull]
        public DecryptedUserKey CurrentDecryptedUserKey
        {
            get => field ?? throw new InvalidOperationException();
            private set;
        }


        public void SetCurrentSession(StoredAccount account, SessionTokens sessionTokens, DecryptedUserKey decryptedUserKey)
        {
            IsAuthenticated = true;
            CurrentUser = account.UserId;
            CurrentContext = new BitwardenClientContext(account.Environment, DeviceIdentity.DeviceInfo);
            CurrentSession = sessionTokens;
            CurrentDecryptedUserKey = decryptedUserKey;
        }

        public void UpdateSession(UserId userId, SessionTokens sessionTokens)
        {
            if (CurrentUser != userId)
                throw new InvalidOperationException();

            CurrentSession = sessionTokens;
        }
    }
}
