using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace FluentBitwarden.Resources.Controls;

[TemplatePart(Name = PartIconPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PartIconDivider, Type = typeof(Border))]
[TemplatePart(Name = PartPasswordTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartRevealButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionDivider, Type = typeof(Border))]
[DependencyProperty<string>("PlaceholderText", DefaultValue = "Enter your password")]
[DependencyProperty<object>("ActionButtonContent")]
[DependencyProperty<string>("ActionButtonToolTip")]
[DependencyProperty<ICommand>("ActionButtonCommand")]
[DependencyProperty<object>("ActionButtonCommandParameter")]
[DependencyProperty<bool>("IsPasswordRevealed")]
[DependencyProperty<object>("Header")]
[DependencyProperty<DataTemplate>("HeaderTemplate")]
[DependencyProperty<ICommand>("Command")]
[DependencyProperty<IconElement>("Icon")]
public sealed partial class PasswordBoxEx : Control
{
    private const string PartIconPresenter = "PART_IconPresenter";
    private const string PartIconDivider = "PART_IconDivider";

    private const string PartPasswordTextBox = "PART_PasswordTextBox";
    private const string PartRevealButton = "PART_RevealButton";
    private const string PartActionButton = "PART_ActionButton";
    private const string PartActionDivider = "PART_ActionDivider";

    private const string RevealGlyph = "\uE890"; // Eye open
    private const string HideGlyph = "\uED1A"; // Eye closed
    private const char PasswordBullet = '\u2022'; // Bullet character

    private ContentControl? _iconPresenter;
    private Border? _iconDivider;

    private TextBox? _passwordTextBox;
    private Button? _revealButton;
    private FontIcon? _revealIcon;

    private Border? _actionDivider;
    private Button? _actionButton;

    private bool _isPointerOver;

    private bool HasInnerFocus =>
        _passwordTextBox?.FocusState != FocusState.Unfocused ||
        _revealButton?.FocusState != FocusState.Unfocused;

    public string Password { get; private set; } = string.Empty;

    public PasswordBoxEx()
    {
        DefaultStyleKey = typeof(PasswordBoxEx);
    }


    partial void OnIconChanged()
    {
        if (_iconPresenter is null || _iconDivider is null)
            return;

        Visibility visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        _iconPresenter.Visibility = visibility;
        _iconDivider.Visibility = visibility;
    }

    partial void OnActionButtonContentChanged()
    {
        if (_actionButton is null || _actionDivider is null)
            return;

        Visibility visibility = ActionButtonContent is null ? Visibility.Collapsed : Visibility.Visible;
        _actionButton.Visibility = visibility;
        _actionDivider.Visibility = visibility;
    }

    partial void OnIsPasswordRevealedChanged()
    {
        SyncRevealDisplay();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_passwordTextBox is not null)
        {
            _passwordTextBox.GotFocus -= OnTextBoxFocusChanged;
            _passwordTextBox.LostFocus -= OnTextBoxFocusChanged;
            _passwordTextBox.TextChanging -= OnTextBoxTextChanging;
        }

        if (_revealButton is not null)
        {
            _revealButton.Click -= OnRevealButtonClick;
        }

        IsEnabledChanged -= OnIsEnabledChanged;
        PointerEntered -= OnControlPointerEntered;
        PointerExited -= OnControlPointerExited;

        _iconPresenter = GetTemplateChild(PartIconPresenter) as ContentControl;
        _iconDivider = GetTemplateChild(PartIconDivider) as Border;

        _passwordTextBox = GetTemplateChild(PartPasswordTextBox) as TextBox;
        _revealButton = GetTemplateChild(PartRevealButton) as Button;
        _revealIcon = _revealButton?.Content as FontIcon;

        _actionButton = GetTemplateChild(PartActionButton) as Button;
        _actionDivider = GetTemplateChild(PartActionDivider) as Border;

        if (_passwordTextBox is not null)
        {
            _passwordTextBox.GotFocus += OnTextBoxFocusChanged;
            _passwordTextBox.LostFocus += OnTextBoxFocusChanged;
            _passwordTextBox.TextChanging += OnTextBoxTextChanging;
        }

        if (_revealButton is not null)
        {
            _revealButton.Click += OnRevealButtonClick;
        }

        IsEnabledChanged += OnIsEnabledChanged;
        PointerEntered += OnControlPointerEntered;
        PointerExited += OnControlPointerExited;

        SyncRevealDisplay();
        UpdateVisualState(false);
        UpdatePasswordState(false);

        OnIconChanged();
        OnActionButtonContentChanged();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        if (ReferenceEquals(e.OriginalSource, this))
        {
            _passwordTextBox?.Focus(FocusState.Programmatic);
        }
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && Command?.CanExecute(Password) == true)
        {
            Command.Execute(Password);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }


    private void OnTextBoxTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (_passwordTextBox is null)
            return;

        try
        {
            if (IsPasswordRevealed)
            {
                Password = _passwordTextBox.Text;
                return;
            }

            int caretPos = _passwordTextBox.SelectionStart;
            Password = RetrievePassword(_passwordTextBox.Text, caretPos, Password);
            _passwordTextBox.TextChanging -= OnTextBoxTextChanging;
            _passwordTextBox.Text = new string(PasswordBullet, Password.Length);
            _passwordTextBox.SelectionStart = caretPos;
            _passwordTextBox.TextChanging += OnTextBoxTextChanging;
        }
        finally
        {
            UpdatePasswordState();
        }
    }

    private void OnRevealButtonClick(object sender, RoutedEventArgs e)
    {
        IsPasswordRevealed = !IsPasswordRevealed;
        _passwordTextBox?.Focus(FocusState.Programmatic);
        SyncRevealDisplay();
    }

    private void OnTextBoxFocusChanged(object sender, RoutedEventArgs e) => UpdateVisualState();
    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateVisualState();

    private void OnControlPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        UpdateVisualState();
    }

    private void OnControlPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        UpdateVisualState();
    }

    private void UpdateVisualState(bool useTransitions = true)
    {
        const string stateNormal = "Normal";
        const string statePointerOver = "PointerOver";
        const string stateFocused = "Focused";
        const string stateDisabled = "Disabled";

        var state = !IsEnabled ? stateDisabled :
            HasInnerFocus ? stateFocused :
            _isPointerOver ? statePointerOver :
            stateNormal;

        VisualStateManager.GoToState(this, state, useTransitions);
    }

    private void UpdatePasswordState(bool useTransitions = true)
    {
        var state = string.IsNullOrEmpty(Password)
            ? "PasswordEmpty"
            : "PasswordNotEmpty";

        VisualStateManager.GoToState(this, state, useTransitions);
    }

    private void SyncRevealDisplay()
    {
        if (_passwordTextBox is null)
            return;

        _revealIcon?.Glyph = IsPasswordRevealed ? HideGlyph : RevealGlyph;
        _passwordTextBox.TextChanging -= OnTextBoxTextChanging;

        _passwordTextBox.Text = IsPasswordRevealed
            ? Password
            : new string(PasswordBullet, Password.Length);

        _passwordTextBox.SelectionStart = _passwordTextBox.Text.Length;
        _passwordTextBox.TextChanging += OnTextBoxTextChanging;
    }

    private static string RetrievePassword(
        ReadOnlySpan<char> bulletsPassword,
        int selectionStart,
        ReadOnlySpan<char> currentPassword)
    {
        int oldLength = currentPassword.Length;
        int newLength = bulletsPassword.Length;
        selectionStart = Math.Clamp(selectionStart, 0, newLength);

        int firstInsertedCharIndex = -1;
        int lastInsertedCharIndex = -1;

        for (int i = 0; i < bulletsPassword.Length; i++)
        {
            if (bulletsPassword[i] != PasswordBullet)
            {
                if (firstInsertedCharIndex == -1)
                    firstInsertedCharIndex = i;

                lastInsertedCharIndex = i;
            }
        }

        // Case 1:
        // User typed or pasted real characters.
        // insertedText = "XYZ"
        if (firstInsertedCharIndex != -1)
        {
            int insertedStart = firstInsertedCharIndex;
            int insertedLength = lastInsertedCharIndex - firstInsertedCharIndex + 1;

            ReadOnlySpan<char> insertedText = bulletsPassword.Slice(insertedStart, insertedLength);
            int removedLength = oldLength + insertedLength - newLength;

            if (removedLength < 0)
                removedLength = 0;

            insertedStart = Math.Clamp(insertedStart, 0, oldLength);
            removedLength = Math.Clamp(removedLength, 0, oldLength - insertedStart);

            return string.Concat(
                currentPassword[..insertedStart],
                insertedText,
                currentPassword[(insertedStart + removedLength)..]);
        }

        // Case 2:
        // No real characters were inserted.
        // User deleted characters.
        if (newLength == oldLength)
        {
            return currentPassword.ToString();
        }

        if (newLength < oldLength)
        {
            int removedLength = oldLength - newLength;
            int removeStart = Math.Clamp(selectionStart, 0, oldLength);
            removedLength = Math.Clamp(removedLength, 0, oldLength - removeStart);

            return string.Concat(
                currentPassword[..removeStart],
                currentPassword[(removeStart + removedLength)..]);
        }

        return currentPassword.ToString();
    }
}