using System.Text;
using Microsoft.UI.Xaml.Documents;

namespace FluentBitwarden.Ui.Features.Vault.CipherDetails;

[TemplateVisualState(Name = StateConcealed, GroupName = GroupRevealStates)]
[TemplateVisualState(Name = StateRevealed, GroupName = GroupRevealStates)]
[DependencyProperty<CipherId>("CipherId")]
[DependencyProperty<string>("Label")]
[DependencyProperty<string>("ActionText")]
[DependencyProperty<string>("Password")]
[DependencyProperty<string>("DisplayText")]
public sealed partial class VaultPasswordField : UserControl
{
    private const string GroupRevealStates = "RevealStates";
    private const string StateConcealed = "Concealed";
    private const string StateRevealed = "Revealed";
    private static readonly string MaskPasswordText = new('\u2022', 10);

    private bool _isRevealed;

    public VaultPasswordField()
    {
        InitializeComponent();
    }

    partial void OnPasswordChanged()
    {
        ResetRevealState();
    }

    partial void OnCipherIdChanged() => ResetRevealState();

    partial void OnLabelChanged()
    {
        if (PART_FieldRow is not null)
            PART_FieldRow.Label = Label ?? string.Empty;
    }

    partial void OnActionTextChanged()
    {
        if (PART_FieldRow is not null)
            PART_FieldRow.ActionText = ActionText ?? string.Empty;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UpdateRevealState(useTransitions: false);
    }

    private void RevealMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _isRevealed = true;
        UpdateRevealState();

    }

    private void ConcealMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _isRevealed = false;
        UpdateRevealState();
    }

    private void UpdateRevealState(bool useTransitions = true)
    {
        UpdateDisplayText();

        VisualStateManager.GoToState(
            this,
            _isRevealed ? StateRevealed : StateConcealed,
            useTransitions);
    }

    private void ResetRevealState()
    {
        _isRevealed = false;
        UpdateRevealState();
    }

    private void UpdateDisplayText()
    {
        PasswordDigits?.Ranges.Clear();
        PasswordSymbols?.Ranges.Clear();

        DisplayText = _isRevealed
            ? Password ?? string.Empty
            : string.IsNullOrEmpty(Password)
                ? string.Empty
                : MaskPasswordText;

        if (!_isRevealed || PasswordDigits is null || PasswordSymbols is null)
            return;

        int start = 0;
        foreach (var character in DisplayText.EnumerateRunes())
        {
            int length = character.Utf16SequenceLength;
            var range = new TextRange { StartIndex = start, Length = length };

            if (Rune.IsDigit(character))
            {
                PasswordDigits.Ranges.Add(range);
            }
            else if (Rune.IsPunctuation(character) || Rune.IsSymbol(character))
            {
                PasswordSymbols.Ranges.Add(range);
            }

            start += length;
        }
    }
}
