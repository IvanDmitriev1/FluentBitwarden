using Microsoft.UI.Xaml.Controls.Primitives;
using BitwardenApi.Primitives;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Vault.Browse.Controls;

[TemplatePart(Name = PartCountdownRing, Type = typeof(CountdownRing))]
[DependencyProperty<TotpValue>("Totp")]
[DependencyProperty<string>("DisplayCode", DefaultValue = "")]
public sealed partial class CipherTotpField : CipherFieldControlBase
{
    private const string PartCountdownRing = "PART_CountdownRing";

    private CountdownRing? _countdownRing;
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
        if (Totp is null)
            return;

        var code = Totp.ComputeTotp();
        CopyTextToClipboard(code);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _visibilityRegistrationToken = RegisterPropertyChangedCallback(
            VisibilityProperty,
            OnVisibilityChanged);

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

        _countdownRing = GetTemplateChild(PartCountdownRing) as CountdownRing;
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
            UpdateTotpDisplay();
        }
    }

    private static string FormatCode(string code) => code.Length switch
    {
        6 => $"{code[..3]} {code[3..]}",
        8 => $"{code[..4]} {code[4..]}",
        _ => code
    };
}
