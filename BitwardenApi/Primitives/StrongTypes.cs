namespace BitwardenApi.Primitives;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct AccessToken;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct RefreshToken;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct DeviceIdentifier;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct DeviceName;

[StronglyTypedId(true, StronglyTypedIdBackingType.Guid, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct CipherId;

[StronglyTypedId(true, StronglyTypedIdBackingType.Guid, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct FolderId;

[StronglyTypedId(true, StronglyTypedIdBackingType.Guid, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct CollectionId;

[StronglyTypedId(true, StronglyTypedIdBackingType.Guid, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct UserId;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct ClientId;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct ClientSecret;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct AuthRequestId;

[StronglyTypedId(true, StronglyTypedIdBackingType.String, StronglyTypedIdJsonConverter.SystemTextJson)]
public partial struct AttachmentId;