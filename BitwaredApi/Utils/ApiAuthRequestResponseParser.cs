using System.Text.Json;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;

namespace BitwaredApi.Utils;

internal static class ApiAuthRequestResponseParser
{
    public static AuthRequestCreateResponse ParseCreateResponse(
        JsonElement root,
        string accessCode,
        DateTimeOffset nowUtc)
    {
        DateTimeOffset created = root.GetOptionalDateTimeOffsetProperty("creationDate") ?? nowUtc;

        return new AuthRequestCreateResponse(
            root.GetRequiredStringProperty("id", "Auth request response did not include an Id."),
            accessCode,
            created.AddMinutes(15));
    }

    public static AuthRequestPollOutcome ParsePollOutcome(JsonElement root, DateTimeOffset nowUtc)
    {
        bool answered = root.TryGetProperty("requestApproved", out JsonElement approvedProp)
            && approvedProp.ValueKind != JsonValueKind.Null;
        bool approved = answered && approvedProp.GetBoolean();
        DateTimeOffset? responseDate = root.GetOptionalDateTimeOffsetProperty("responseDate");
        DateTimeOffset creationDate = root.GetOptionalDateTimeOffsetProperty("creationDate") ?? nowUtc;

        if (creationDate.AddMinutes(15) <= nowUtc)
        {
            return new AuthRequestPollOutcome.Expired("The device login request expired before approval.");
        }

        if (!answered)
        {
            return new AuthRequestPollOutcome.Pending();
        }

        string? encryptedUserKey = root.GetOptionalStringProperty("key");
        if (!approved || string.IsNullOrWhiteSpace(encryptedUserKey))
        {
            return new AuthRequestPollOutcome.Denied("The device login request was denied.");
        }

        return new AuthRequestPollOutcome.Approved(
            new AuthRequestApproval(
                encryptedUserKey,
                responseDate,
                root.GetOptionalStringProperty("requestDeviceIdentifier"),
                root.GetOptionalStringProperty("requestIpAddress"),
                root.GetOptionalStringProperty("requestCountryName")));
    }
}
