using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Diagnostics.CodeAnalysis;

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
        public SessionTokens SessionTokens
        {
            get => field ?? throw new InvalidOperationException();
            private set;
        }


        public void SetCurrentSession(UserId currentUser, BitwardenClientContext context, SessionTokens sessionTokens)
        {
            IsAuthenticated = true;
            CurrentUser = currentUser;
            SessionTokens = sessionTokens;
            CurrentContext = context;
        }
    }
}
