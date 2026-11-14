using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Windows.SDK.BuildTools.WinApp.UIAutomation;

namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

public sealed class DesktopAutomation(IUiAutomation ui, UiTarget target)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private const int ElementNotAvailable = unchecked((int)0x80040201);

    public async Task<UiElement> WaitAsync(
        UiSelector selector,
        string condition = "is present",
        Func<UiElement, bool>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var testToken = TestContext.Current.CancellationToken;
        using var deadline = new CancellationTokenSource(Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(testToken, cancellationToken, deadline.Token);
        long started = Stopwatch.GetTimestamp();
        string lastState = "no UI Automation result";

        try
        {
            while (Stopwatch.GetElapsedTime(started) < Timeout)
            {
                linked.Token.ThrowIfCancellationRequested();
                try
                {
                    var element = await ui.FindSingleElementAsync(target, selector, requireUnique: true, ct: linked.Token);
                    if (element is null)
                    {
                        lastState = "selector absent";
                    }
                    else
                    {
                        lastState = $"present; enabled={element.IsEnabled}; offscreen={element.IsOffscreen}; name='{element.Name}'";
                        if (predicate is null || predicate(element))
                        {
                            if (Stopwatch.GetElapsedTime(started) >= Timeout)
                                throw CreateTimeoutException(selector, condition, lastState);
                            return element;
                        }
                    }
                }
                catch (UiElementNotFoundException exception)
                {
                    lastState = $"selector absent ({exception.Message})";
                    // The element can appear after navigation or layout completes.
                }
                catch (UiElementOffscreenException exception)
                {
                    lastState = $"offscreen ({exception.Message})";
                    // Layout can temporarily report an element outside the visible window.
                }
                catch (COMException exception) when (exception.HResult == ElementNotAvailable)
                {
                    lastState = $"UIA element unavailable (0x{exception.HResult:X8}: {exception.Message})";
                    // UIA can invalidate the root while navigation is replacing its children.
                }

                TimeSpan remaining = Timeout - Stopwatch.GetElapsedTime(started);
                if (remaining <= TimeSpan.Zero)
                    break;

                await Task.Delay(remaining < PollInterval ? remaining : PollInterval, linked.Token);
            }
        }
        catch (OperationCanceledException) when (
            !testToken.IsCancellationRequested &&
            !cancellationToken.IsCancellationRequested &&
            deadline.IsCancellationRequested)
        {
            throw CreateTimeoutException(selector, condition, lastState);
        }

        throw CreateTimeoutException(selector, condition, lastState);
    }

    public async Task WaitAbsentAsync(UiSelector selector, CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        try
        {
            var element = await ui.FindSingleElementAsync(target, selector, requireUnique: true, ct: linked.Token);
            Assert.Null(element);
        }
        catch (UiElementNotFoundException)
        {
        }
    }

    public async Task SetValueAsync(UiElement element, string value, CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        await ui.SetValueAsync(target, element, value, linked.Token);
    }

    public async Task FocusAsync(UiElement element, CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        await ui.FocusAsync(target, element, linked.Token);
    }

    public async Task<object?> PropertyAsync(
        UiElement element,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        var properties = await ui.GetPropertiesAsync(target, element, propertyName, linked.Token);
        return properties.GetValueOrDefault(propertyName);
    }

    public async Task InvokeAsync(UiElement element, CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, cancellationToken);
        await ui.InvokeAsync(target, element, linked.Token);
    }

    private static TimeoutException CreateTimeoutException(UiSelector selector, string condition, string lastState)
    {
        var identity = selector.Slug ?? selector.Query ?? selector.ControlType ?? selector.ClassName ?? "any element";
        var root = selector.Root is null
            ? "none"
            : selector.Root.Slug ?? selector.Root.Query ?? selector.Root.ControlType ?? selector.Root.ClassName ?? "unspecified";
        return new TimeoutException($"Timed out after {Timeout.TotalSeconds:0.##} seconds waiting for selector '{identity}' under root '{root}' to be {condition}. Last UI Automation state: {lastState}.");
    }
}
