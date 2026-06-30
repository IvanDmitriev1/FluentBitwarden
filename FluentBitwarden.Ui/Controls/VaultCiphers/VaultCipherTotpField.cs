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
    private readonly DependencyPropertyCallbackRegistration _visibilityCallbackRegistration;

    public VaultCipherTotpField()
    {
        DefaultStyleKey = typeof(VaultCipherTotpField);

        _visibilityCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            VisibilityProperty,
            static (sender, dp) => ((VaultCipherTotpField)sender).OnVisibilityChanged(dp));
    }

    partial void OnTotpChanged()
    {
        UpdateTotpDisplay();
        UpdateTimerState();
    }

    protected override void OnApplyTemplate()
    {
        DetachTemplateSubscriptions();

        base.OnApplyTemplate();

        _countdownRing = GetTemplateChild(PartCountdownRing) as CountdownRing;

        _visibilityCallbackRegistration.Register();
        EnsureTimer();
        UpdateTotpDisplay();
        UpdateTimerState();
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

    private void OnTimerTick(DispatcherQueueTimer sender, object args) => UpdateTotpDisplay();

    private void OnVisibilityChanged(DependencyProperty dp)
    {
        UpdateTimerState();

        if (Visibility == Visibility.Visible)
            UpdateTotpDisplay();
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

    private void EnsureTimer()
    {
        if (_timer is not null)
            return;

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += OnTimerTick;
    }

    private void DetachTemplateSubscriptions()
    {
        _visibilityCallbackRegistration.Unregister();

        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        _countdownRing = null;
    }

    private void UpdateTimerState()
    {
        if (_timer is null)
            return;

        if (Visibility == Visibility.Visible && Totp is not null && _countdownRing is not null)
        {
            _timer.Start();
            return;
        }

        _timer.Stop();
    }
}
