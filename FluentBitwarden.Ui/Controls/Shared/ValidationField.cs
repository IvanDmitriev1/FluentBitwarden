using Microsoft.UI.Xaml;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.Controls.Shared;

[DependencyProperty<ValidatableProperty>("Property")]
[DependencyProperty<string>("CurrentErrorMessage", DefaultValue = "")]
[DependencyProperty<Visibility>("ErrorVisibility", DefaultValue = Visibility.Collapsed)]
public sealed partial class ValidationField : ContentControl
{
    public ValidationField()
    {
        DefaultStyleKey = typeof(ValidationField);
    }

    partial void OnPropertyChanged(ValidatableProperty? oldValue, ValidatableProperty? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Source.ErrorsChanged -= OnErrorsChanged;
        }

        if (newValue is not null)
        {
            newValue.Source.ErrorsChanged += OnErrorsChanged;
        }

        RefreshValidationState();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        RefreshValidationState();
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        if (!ShouldRefreshFor(e.PropertyName))
        {
            return;
        }

        if (DispatcherQueue is { HasThreadAccess: false } dispatcherQueue)
        {
            _ = dispatcherQueue.TryEnqueue(RefreshValidationState);
            return;
        }

        RefreshValidationState();
    }

    private bool ShouldRefreshFor(string? propertyName)
        => string.IsNullOrWhiteSpace(propertyName)
           || string.Equals(propertyName, Property?.PropertyName, StringComparison.Ordinal);

    private void RefreshValidationState()
    {
        INotifyDataErrorInfo? validationSource = Property?.Source;
        string propertyName = Property?.PropertyName ?? string.Empty;

        if (validationSource is null || string.IsNullOrWhiteSpace(propertyName))
        {
            ClearValidationState();
            return;
        }

        string errorMessage = GetFirstErrorMessage(validationSource.GetErrors(propertyName));
        if (string.IsNullOrEmpty(errorMessage))
        {
            ClearValidationState();
            return;
        }

        CurrentErrorMessage = errorMessage;
        ErrorVisibility = Visibility.Visible;
    }

    private static string GetFirstErrorMessage(IEnumerable? errors)
    {
        if (errors is null)
        {
            return string.Empty;
        }

        foreach (object? error in errors)
        {
            string? errorMessage = error switch
            {
                ValidationResult { ErrorMessage: { } message } when !string.IsNullOrWhiteSpace(message) => message,
                string message when !string.IsNullOrWhiteSpace(message) => message,
                not null => error.ToString(),
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return errorMessage;
            }
        }

        return string.Empty;
    }

    private void ClearValidationState()
    {
        CurrentErrorMessage = string.Empty;
        ErrorVisibility = Visibility.Collapsed;
    }
}
