namespace BitwardenApi.Modules.Identity.Models;

[StronglyTypedId(Template.String)]
public readonly partial struct AccessToken;

[StronglyTypedId(Template.String)]
public readonly partial struct RefreshToken;

[StronglyTypedId(Template.String)]
public readonly partial struct TwoFactorToken;

[StronglyTypedId(Template.String)]
public readonly partial struct EncryptedUserKey;

[StronglyTypedId(Template.String)]
public readonly partial struct EncryptedPrivateKey;


[StronglyTypedId(Template.Guid)]
public readonly partial struct UserId;

[StronglyTypedId(Template.String)]
public readonly partial struct AuthRequestId;

[StronglyTypedId(Template.Guid)]
public readonly partial struct OrganizationId;