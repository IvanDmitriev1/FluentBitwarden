namespace BitwardenApi.Modules.Identity.Models;

[StronglyTypedId(Template.String)]
public readonly partial struct AccessToken;

[StronglyTypedId(Template.String)]
public readonly partial struct RefreshToken;

[StronglyTypedId(Template.String)]
public readonly partial struct TwoFactorToken;

[StronglyTypedId(Template.String, "string-dapper")]
public readonly partial struct EncryptedUserKey;

[StronglyTypedId(Template.String, "string-dapper")]
public readonly partial struct EncryptedPrivateKey;


[StronglyTypedId(Template.Guid, "guid-dapper")]
public readonly partial struct UserId;

[StronglyTypedId(Template.String)]
public readonly partial struct AuthRequestId;
