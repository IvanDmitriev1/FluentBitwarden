namespace BitwardenApi.Shared.Context;

[StronglyTypedId(Template.String)]
public readonly partial struct DeviceIdentifier;

[StronglyTypedId(Template.String)]
public readonly partial struct DeviceName;

public readonly record struct DeviceInfo(
    DeviceIdentifier DeviceIdentifier,
    DeviceName DeviceName);
