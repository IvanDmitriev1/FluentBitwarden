using System.Reflection;

namespace FluentBitwarden.Platform.Ipc.Models;

internal sealed record IpcRpcHandlerMethodDescriptor(
    ushort MessageType,
    IpcAuthenticationLevel AuthenticationLevel,
    IpcRpcHandlerMethodKind Kind,
    MethodInfo Method,
    [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? ResponseType,
    [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? RequestType);
