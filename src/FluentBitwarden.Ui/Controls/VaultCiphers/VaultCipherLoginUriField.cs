using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Exception = System.Exception;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartChrome, Type = typeof(VaultCipherFieldChrome))]
[TemplatePart(Name = PartUriPrefixRun, Type = typeof(Run))]
[TemplatePart(Name = PartUriDomainRun, Type = typeof(Run))]
[TemplatePart(Name = PartUriSuffixRun, Type = typeof(Run))]
[TemplateVisualState(Name = StateNormal, GroupName = GroupCommonStates)]
[TemplateVisualState(Name = StatePointerOver, GroupName = GroupCommonStates)]
[TemplateVisualState(Name = StatePressed, GroupName = GroupCommonStates)]
[TemplateVisualState(Name = StateDisabled, GroupName = GroupCommonStates)]
[DependencyProperty<LoginUri>("Uri")]
[DependencyProperty<Brush>("DomainForeground")]
[DependencyProperty<Brush>("SecondaryForeground")]
public sealed partial class VaultCipherLoginUriField : Control
{
    private readonly record struct UriParts(string Prefix, string Domain, string Suffix);

    private const string PartChrome = "PART_Chrome";
    private const string PartUriPrefixRun = "PART_UriPrefixRun";
    private const string PartUriDomainRun = "PART_UriDomainRun";
    private const string PartUriSuffixRun = "PART_UriSuffixRun";

    private const string GroupCommonStates = "CommonStates";
    private const string StateNormal = "Normal";
    private const string StatePointerOver = "PointerOver";
    private const string StatePressed = "Pressed";
    private const string StateDisabled = "Disabled";

    private readonly DependencyPropertyCallbackRegistration _isEnabledCallbackRegistration;

    private bool _isPointerOver;
    private bool _isPressed;
    private VaultCipherFieldChrome? _chrome;
    private Run? _uriPrefixRun;
    private Run? _uriDomainRun;
    private Run? _uriSuffixRun;

    public VaultCipherLoginUriField()
    {
        DefaultStyleKey = typeof(VaultCipherLoginUriField);
        _isEnabledCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            IsEnabledProperty,
            static (sender, dp) => ((VaultCipherLoginUriField)sender).OnIsEnabledChanged(dp));
    }

    protected override void OnApplyTemplate()
    {
        _isEnabledCallbackRegistration.Unregister();
        _chrome?.Click -= OnChromeClick;

        base.OnApplyTemplate();

        _chrome = GetTemplateChild(PartChrome) as VaultCipherFieldChrome;
        _uriPrefixRun = GetTemplateChild(PartUriPrefixRun) as Run;
        _uriDomainRun = GetTemplateChild(PartUriDomainRun) as Run;
        _uriSuffixRun = GetTemplateChild(PartUriSuffixRun) as Run;

        _chrome?.Click += OnChromeClick;
        _isEnabledCallbackRegistration.Register();
        UpdateUriParts();
        UpdateVisualState(useTransitions: false);
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs args)
    {
        base.OnPointerEntered(args);

        _isPointerOver = true;
        UpdateVisualState();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs args)
    {
        base.OnPointerExited(args);

        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualState();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs args)
    {
        base.OnPointerPressed(args);

        _isPressed = true;
        UpdateVisualState();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs args)
    {
        base.OnPointerReleased(args);

        _isPressed = false;
        UpdateVisualState();
    }

    partial void OnUriChanged() => UpdateUriParts();
    partial void OnDomainForegroundChanged() => UpdateVisualState();
    partial void OnSecondaryForegroundChanged() => UpdateVisualState();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "async void UI event handler; unhandled exceptions here would crash the process instead of being logged.")]
    private async void OnChromeClick(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (Uri is null || !Uri.TryGetWebUri(out var launchUri))
            return;

        try
        {
            await Launcher.LaunchUriAsync(launchUri);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to launch URI '{launchUri}': {exception}");
        }
    }
    private void UpdateUriParts()
    {
        if (Uri is not { } loginUri || string.IsNullOrWhiteSpace(loginUri.Value))
        {
            SetUriParts(string.Empty, string.Empty, string.Empty);
            return;
        }

        if (!TryCreateUriParts(loginUri, out var parts))
        {
            SetUriParts(loginUri.Value, string.Empty, string.Empty);
            return;
        }

        SetUriParts(parts.Prefix, parts.Domain, parts.Suffix);
    }

    private void SetUriParts(string prefix, string domain, string suffix)
    {
        _uriPrefixRun?.Text = prefix;
        _uriDomainRun?.Text = domain;
        _uriSuffixRun?.Text = suffix;
    }

    private void OnIsEnabledChanged(DependencyProperty dp)
    {
        if (!IsEnabled)
        {
            _isPressed = false;
        }

        UpdateVisualState();
    }

    private void UpdateVisualState(bool useTransitions = true)
    {
        string state;

        if (!IsEnabled)
            state = StateDisabled;
        else if (_isPressed)
            state = StatePressed;
        else if (_isPointerOver)
            state = StatePointerOver;
        else
            state = StateNormal;

        VisualStateManager.GoToState(this, state, useTransitions);
    }

    private static bool TryCreateUriParts(LoginUri loginUri, out UriParts parts)
    {
        parts = default;

        if (loginUri.TryGetAbsoluteUri(out var absoluteUri) && !loginUri.IsWebUri)
        {
            if (string.IsNullOrWhiteSpace(absoluteUri.Host))
                return false;

            parts = new UriParts(string.Empty, absoluteUri.Host, string.Empty);
            return true;
        }

        if (!loginUri.TryGetWebUri(out var webUri) || string.IsNullOrWhiteSpace(webUri.Host))
            return false;

        var hostStart = loginUri.Value.IndexOf(webUri.Host, StringComparison.OrdinalIgnoreCase);
        if (hostStart < 0)
            return false;

        parts = new UriParts(
            loginUri.Value[..hostStart],
            loginUri.Value.Substring(hostStart, webUri.Host.Length),
            loginUri.Value[(hostStart + webUri.Host.Length)..]);

        return true;
    }
}
