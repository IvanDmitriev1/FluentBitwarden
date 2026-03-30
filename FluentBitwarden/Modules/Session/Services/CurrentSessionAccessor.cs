using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session.Models.Unlock;

namespace FluentBitwarden.Modules.Session.Services
{
    internal sealed class CurrentSessionAccessor : ICurrentSessionAccessor
    {
        public bool IsAuthenticated { get; private set; }

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
        public UserKeySession CurrentUserKeySession
        {
            get => field ?? throw new InvalidOperationException();
            private set;
        }


        public void SetCurrentSession(StoredAccount account, SessionTokens sessionTokens, UserKeySession userKeySession)
        {
            IsAuthenticated = true;
            CurrentUser = account.UserId;
            CurrentContext = new BitwardenClientContext(account.Environment, DeviceIdentity.DeviceInfo);
            CurrentSession = sessionTokens;
            CurrentUserKeySession = userKeySession;
        }

        public void UpdateSession(UserId userId, SessionTokens sessionTokens)
        {
            if (CurrentUser != userId)
                throw new InvalidOperationException();

            CurrentSession = sessionTokens;
        }
    }
}
