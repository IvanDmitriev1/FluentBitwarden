using System.Data;

namespace FluentBitwarden.AppHost.Infrastructure.Extensions;

internal static class DbSessionExtensions
{
    extension(IDbSession dbSession)
    {
        public IDbTransaction RequiredTransaction => dbSession.Transaction ??
                                                     throw new InvalidOperationException(
                                                         "A transaction is required but none is present.");
    }
}
