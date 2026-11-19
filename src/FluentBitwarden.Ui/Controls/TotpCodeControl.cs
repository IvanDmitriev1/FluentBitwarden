using FluentBitwarden.Ui.Infrastructure;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Ui.Controls;

[TemplatePart(Name = PartCode, Type = typeof(TextBlock))]
[TemplatePart(Name = PartCountdownRing, Type = typeof(CountdownRing))]
[DependencyProperty<TotpValue>("Totp")]
[DependencyProperty<bool>("HideCode")]
public sealed partial class TotpCodeControl : Control
{
    private const string PartCode = "PART_Code";
    private const string PartCountdownRing = "PART_CountdownRing";


    private readonly DependencyPropertyCallbackRegistration _visibilityCallbackRegistration;
    private readonly DispatcherQueueTimer _timer;
    private TextBlock? _codeTextBlock;
    private CountdownRing? _countdownRing;

    public TotpCodeControl()
    {
        DefaultStyleKey = typeof(TotpCodeControl);

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;

        _visibilityCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            VisibilityProperty,
            static (sender, dp) => ((TotpCodeControl)sender).OnVisibilityChanged());
    }

    partial void OnTotpChanged(TotpValue? newValue)
    {
        UpdateState();
    }

    protected override void OnApplyTemplate()
    {
        StopTimer();
        _visibilityCallbackRegistration.Unregister();

        base.OnApplyTemplate();

        _codeTextBlock = GetTemplateChild(PartCode) as TextBlock;
        _countdownRing = GetTemplateChild(PartCountdownRing) as CountdownRing;

        _visibilityCallbackRegistration.Register();
        UpdateState();
    }

    private void OnVisibilityChanged()
    {
        UpdateState();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        var totp = Totp;
        if (totp is null)
        {
            StopTimer();
            _codeTextBlock?.Text = string.Empty;
            _countdownRing?.Value = 0;
            return;
        }

        if (Visibility != Visibility.Visible)
        {
            StopTimer();
            return;
        }

        if (_codeTextBlock is null || _countdownRing is null)
            return;

        var remaining = totp.ExpiresAt - DateTimeOffset.UtcNow;
        var code = totp.ToString();

        _countdownRing.Maximum = totp.Step;
        _countdownRing.Value = Math.Clamp((int)Math.Ceiling(remaining.TotalSeconds), 0, totp.Step);
        _codeTextBlock.Text = HideCode ? FormatCode(code) : code;

        StartTimer();
    }

    private void StartTimer()
    {
        if (_timer.IsRunning || !IsLoaded)
            return;

        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (!_timer.IsRunning || !IsLoaded)
            return;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private static string FormatCode(string code) => code.Length switch
    {
        6 => $"{code[..3]} {code[3..]}",
        8 => $"{code[..4]} {code[4..]}",
        _ => code
    };

}
