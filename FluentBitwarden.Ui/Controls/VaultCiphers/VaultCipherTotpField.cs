using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using OtpNet;
using FluentBitwarden.Controls.Shared;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartCountdownRing, Type = typeof(CountdownRing))]
[DependencyProperty<TotpValue>("Totp")]
[DependencyProperty<string>("DisplayCode", DefaultValue = "")]
public sealed partial class VaultCipherTotpField : VaultCipherFieldControlBase
{
    private const string PartCountdownRing = "PART_CountdownRing";

    private CountdownRing? _countdownRing;
    private DispatcherQueueTimer? _timer;
    private long _visibilityRegistrationToken;
    private bool _isVisibilityCallbackRegistered;

    public VaultCipherTotpField()
    {
        DefaultStyleKey = typeof(VaultCipherTotpField);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    partial void OnTotpChanged()
    {
        UpdateTotpDisplay();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _countdownRing = GetTemplateChild(PartCountdownRing) as CountdownRing;
        UpdateTotpDisplay();
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction()
    {
        if (Totp is null)
            return;

        var code = Totp.ComputeTotp();
        CopyTextToClipboard(code);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isVisibilityCallbackRegistered)
        {
            _visibilityRegistrationToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
            _isVisibilityCallbackRegistered = true;
        }

        EnsureTimer();
        UpdateTimerState();

        UpdateTotpDisplay();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isVisibilityCallbackRegistered)
        {
            UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityRegistrationToken);
            _isVisibilityCallbackRegistered = false;
        }

        _timer?.Stop();
    }

    private void UpdateTotpDisplay()
    {
        if (_countdownRing is null || Totp is null)
        {
            DisplayCode = string.Empty;
            _countdownRing?.Value = 0;
            _countdownRing?.Maximum = 1;
            return;
        }

        DisplayCode = FormatCode(Totp.ComputeTotp());

        var remaining = Totp.ExpiresAt - DateTimeOffset.UtcNow;
        var remainingSeconds = Math.Clamp((int)Math.Ceiling(remaining.TotalSeconds), 0, Totp.Step);

        _countdownRing.Maximum = Totp.Step;
        _countdownRing.Value = remainingSeconds;
        return;

        static string FormatCode(string code) => code.Length switch
        {
            6 => $"{code[..3]} {code[3..]}",
            8 => $"{code[..4]} {code[4..]}",
            _ => code
        };
    }

    private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateTimerState();

        if (Visibility == Visibility.Visible)
            UpdateTotpDisplay();
    }

    private void EnsureTimer()
    {
        if (_timer is not null)
            return;

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += OnTimerTick;
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args) => UpdateTotpDisplay();

    private void UpdateTimerState()
    {
        if (_timer is null)
            return;

        if (Visibility == Visibility.Visible)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }
}
