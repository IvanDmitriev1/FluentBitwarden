using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface IAuthenticationWorkflow
{
    ValueTask<PasswordSignInOutcome> SignInWithPasswordAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<AuthenticationOutcome> ContinueTwoFactorAsync(
        TwoFactorSignInRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DeviceLoginStartResult> StartDeviceLoginAsync(
        DeviceLoginStartRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DeviceApprovalOutcome> PollDeviceLoginAsync(
        DeviceLoginPollRequest request,
        CancellationToken cancellationToken = default);
}
