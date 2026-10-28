using BitwardenApi.Notifications.Internal;
using System.Text.Json.Serialization;

namespace BitwardenApi.Notifications.Models;

public readonly record struct CipherChangedNotification(
    CipherId Id,
    [property: JsonConverter(typeof(OptionalCipherNotificationUserIdJsonConverter))] UserId UserId,
    [property: JsonConverter(typeof(OptionalCipherNotificationOrganizationIdJsonConverter))] OrganizationId OrganizationId,
    CollectionId[] CollectionIds,
    DateTimeOffset RevisionDate);
