using BitwaredApi;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Extensions;

public static class StoredSessionInfoExtensions
{
    public static string DescribeEnvironment(this StoredSessionInfo session)
    {
        string host = session.Environment.ApiBase.Host;

        if (session.Environment == BitwardenEnvironment.Europe)
        {
            return "Bitwarden EU";
        }

        if (session.Environment == BitwardenEnvironment.UnitedStates)
        {
            return "Bitwarden US";
        }

        return host;
    }
}