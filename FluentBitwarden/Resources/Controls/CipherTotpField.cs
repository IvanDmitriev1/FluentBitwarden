using FluentBitwarden.Shared.Totp;
using Microsoft.UI.Xaml.Controls.Primitives;
using OtpNet;
using System.Diagnostics;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Resources.Controls;

[TemplatePart(Name = PartCountdownRing, Type = typeof(CountdownRing))]
[DependencyProperty<string>("Totp", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("DisplayCode", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneWay)]
public sealed partial class CipherTotpField : CipherFieldControlBase
{
    private const string PartCountdownRing = "PART_CountdownRing";

    private CountdownRing? _countdownRing;
    private Totp? _totp;
    private DispatcherQueueTimer? _timer;
    private long _visibilityRegistrationToken;

    public CipherTotpField()
    {
        DefaultStyleKey = typeof(CipherTotpField);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    partial void OnTotpChanged()
    {
        UpdateTotpDisplay();
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction()
    {
        if (_totp is null)
            return;

        var code = _totp.ComputeTotp();
        CopyTextToClipboard(code);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _visibilityRegistrationToken = RegisterPropertyChangedCallback(
            VisibilityProperty,
            OnVisibilityChanged);

        _timer = DispatcherQueue.CreateTimer();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => UpdateTotpDisplay();
        _timer.Start();

        UpdateTotpDisplay();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityRegistrationToken);

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;

        _timer?.Stop();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _countdownRing = GetTemplateChild("PART_CountdownRing") as CountdownRing;
    }

    partial void OnTotpChanged(string? newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
            return;

        _totp = CreateTopt(newValue);
    }

    private void UpdateTotpDisplay()
    {
        if (_countdownRing is null || _totp is null)
        {
            DisplayCode = string.Empty;
            _countdownRing?.Value = 0;
            _countdownRing?.Maximum = 1;
            return;
        }

        var utcNow = DateTime.UtcNow;
        DisplayCode = FormatCode(_totp.ComputeTotp(utcNow));

        int remainingSeconds = GetRemainingSeconds(utcNow, _totp.Step);
        _countdownRing.Maximum = _totp.Step;
        _countdownRing.Value = remainingSeconds;
    }

    private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (Visibility == Visibility.Collapsed)
        {
            _timer?.Stop();
        }
        else
        {
            _timer?.Start();
        }
    }

    private static Totp CreateTopt(string value)
    {
        if (OtpAuthUriParser.TryParse(value, out var otpAuth))
        {
            Debug.Assert(otpAuth.Type == OtpType.Totp);

            var secretBytes = Base32Encoding.ToBytes(otpAuth.Secret);

            return new Totp(
                secretBytes,
                step: otpAuth.PeriodSeconds,
                mode: otpAuth.Algorithm,
                totpSize: otpAuth.Digits);
        }

        return new Totp(
            Base32Encoding.ToBytes(value),
            step: 30,
            mode: OtpHashMode.Sha1,
            totpSize: 6);
    }

    private static int GetRemainingSeconds(DateTime utcNow, int periodSeconds)
    {
        var unixSeconds = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
        var elapsedInWindow = unixSeconds % periodSeconds;

        int remaining = periodSeconds - (int)elapsedInWindow;
        if (remaining <= 0)
        {
            remaining = periodSeconds;
        }

        return remaining;
    }

    private static string FormatCode(string code) => code.Length switch
    {
        6 => $"{code[..3]} {code[3..]}",
        8 => $"{code[..4]} {code[4..]}",
        _ => code
    };
}