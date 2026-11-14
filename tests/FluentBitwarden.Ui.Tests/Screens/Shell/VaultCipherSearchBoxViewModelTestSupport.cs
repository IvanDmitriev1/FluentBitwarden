using BitwardenApi.Vault.Ciphers.Models;

namespace FluentBitwarden.Ui.Tests.Screens.Shell;

internal static class VaultCipherSearchBoxViewModelTestSupport
{
    public static TaskCompletionSource<T> NewSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Task<Cipher[]> StartSearch(
        TaskCompletionSource<bool> started,
        TaskCompletionSource<Cipher[]> response)
    {
        started.TrySetResult(true);
        return response.Task;
    }

    public static Task<Cipher[]> StartSearch(
        TaskCompletionSource<CancellationToken> started,
        TaskCompletionSource<Cipher[]> response,
        CancellationToken token)
    {
        started.TrySetResult(token);
        return response.Task;
    }

    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken,
            deadline.Token);
        while (!condition())
            await Task.Delay(10, linked.Token);
    }

    public static Cipher CreateCipher(string name)
    {
        Cipher cipher = Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Name = name;
        return cipher;
    }
}
