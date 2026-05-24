using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace FluentBitwarden.UI.Controls;

[TemplatePart(Name = PartPasswordTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartRevealButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionButton, Type = typeof(Button))]
[TemplatePart(Name = PartHeaderContentPresenter, Type = typeof(ContentPresenter))]
[DependencyProperty<string>("PlaceholderText", DefaultValue = "Enter your password")]
[DependencyProperty<object>("ActionButtonContent")]
[DependencyProperty<string>("ActionButtonToolTip")]
[DependencyProperty<ICommand>("ActionButtonCommand")]
[DependencyProperty<object>("ActionButtonCommandParameter")]
[DependencyProperty<bool>("IsPasswordRevealed")]
[DependencyProperty<object>("Header")]
[DependencyProperty<DataTemplate>("HeaderTemplate")]
[DependencyProperty<ICommand>("Command")]
public sealed partial class PasswordBoxEx : Control
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(PasswordBoxEx),
            new PropertyMetadata(string.Empty, OnPasswordPropertyChanged));

    private const string PartPasswordTextBox = "PART_PasswordTextBox";
    private const string PartRevealButton = "PART_RevealButton";
    private const string PartActionButton = "PART_ActionButton";
    private const string PartHeaderContentPresenter = "HeaderContentPresenter";

    private const string RevealGlyph = "\uE890"; // Eye open
    private const string HideGlyph = "\uED1A"; // Eye closed
    private const char PasswordBullet = '\u2022'; // Bullet character

    private TextBox? _passwordTextBox;
    private Button? _revealButton;
    private FontIcon? _revealIcon;
    private Button? _actionButton;
    private ContentPresenter? _headerContentPresenter;

    private bool _isPointerOver;

    private bool HasInnerFocus =>
        _passwordTextBox?.FocusState != FocusState.Unfocused ||
        _revealButton?.FocusState != FocusState.Unfocused ||
        _actionButton?.FocusState != FocusState.Unfocused;

    public EventHandler<PasswordBoxEx, string>? PasswordChanged;

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        private set => SetValue(PasswordProperty, value);
    }

    public PasswordBoxEx()
    {
        DefaultStyleKey = typeof(PasswordBoxEx);
    }

    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PasswordBoxEx)d;
        var password = (string?)e.NewValue ?? string.Empty;

        control.PasswordChanged?.Invoke(control, password);
        control.UpdatePasswordState();
    }

    partial void OnActionButtonContentChanged()
    {
        if (_actionButton is null)
            return;

        Visibility visibility = ActionButtonContent is null ? Visibility.Collapsed : Visibility.Visible;
        _actionButton.Visibility = visibility;
    }

    partial void OnHeaderChanged()
    {
        if (_headerContentPresenter is null)
            return;

        Visibility visibility = Header is null ? Visibility.Collapsed : Visibility.Visible;
        _headerContentPresenter.Visibility = visibility;
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
            _revealButton.GotFocus -= OnButtonFocusChanged;
            _revealButton.LostFocus -= OnButtonFocusChanged;
            _revealButton.Click -= OnRevealButtonClick;
        }

        if (_actionButton is not null)
        {
            _actionButton.GotFocus -= OnButtonFocusChanged;
            _actionButton.LostFocus -= OnButtonFocusChanged;
        }

        IsEnabledChanged -= OnIsEnabledChanged;
        PointerEntered -= OnControlPointerEntered;
        PointerExited -= OnControlPointerExited;

        _passwordTextBox = GetTemplateChild(PartPasswordTextBox) as TextBox;
        _revealButton = GetTemplateChild(PartRevealButton) as Button;
        _revealIcon = _revealButton?.Content as FontIcon;
        _actionButton = GetTemplateChild(PartActionButton) as Button;
        _headerContentPresenter = GetTemplateChild(PartHeaderContentPresenter) as ContentPresenter;

        if (_passwordTextBox is not null)
        {
            _passwordTextBox.GotFocus += OnTextBoxFocusChanged;
            _passwordTextBox.LostFocus += OnTextBoxFocusChanged;
            _passwordTextBox.TextChanging += OnTextBoxTextChanging;
        }

        if (_revealButton is not null)
        {
            _revealButton.GotFocus += OnButtonFocusChanged;
            _revealButton.LostFocus += OnButtonFocusChanged;
            _revealButton.Click += OnRevealButtonClick;
        }

        if (_actionButton is not null)
        {
            _actionButton.GotFocus += OnButtonFocusChanged;
            _actionButton.LostFocus += OnButtonFocusChanged;
        }

        IsEnabledChanged += OnIsEnabledChanged;
        PointerEntered += OnControlPointerEntered;
        PointerExited += OnControlPointerExited;

        SyncRevealDisplay();
        OnHeaderChanged();
        OnActionButtonContentChanged();

        UpdateVisualState(false);
        UpdatePasswordState(false);
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

    private void OnRevealButtonClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Password))
            return;

        IsPasswordRevealed = !IsPasswordRevealed;
        _passwordTextBox?.Focus(FocusState.Programmatic);
    }

    private void OnTextBoxFocusChanged(object sender, RoutedEventArgs e) => UpdateVisualState();
    private void OnButtonFocusChanged(object sender, RoutedEventArgs e) => UpdateVisualState();
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
