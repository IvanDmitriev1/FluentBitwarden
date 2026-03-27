using Microsoft.UI.Xaml;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.Resources.Controls;

[DependencyProperty<ValidatableProperty>("Property")]
[DependencyProperty<string>("CurrentErrorMessage", DefaultValue = "")]
[DependencyProperty<Visibility>("ErrorVisibility", DefaultValue = Visibility.Collapsed)]
public sealed partial class ValidationField : ContentControl
{
    private INotifyDataErrorInfo? _currentValidationSource;
    private string _currentPropertyName = string.Empty;

    public ValidationField()
    {
        DefaultStyleKey = typeof(ValidationField);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    partial void OnPropertyChanged(ValidatableProperty? oldValue, ValidatableProperty? newValue)
        => RefreshValidationSubscription();

    private void OnLoaded(object sender, RoutedEventArgs e)
        => RefreshValidationSubscription();

    private void OnUnloaded(object sender, RoutedEventArgs e)
        => DetachValidationSource();

    private void RefreshValidationSubscription()
    {
        INotifyDataErrorInfo? nextValidationSource = Property?.Source;
        string nextPropertyName = Property?.PropertyName ?? string.Empty;

        if (!ReferenceEquals(_currentValidationSource, nextValidationSource)
            || !string.Equals(_currentPropertyName, nextPropertyName, StringComparison.Ordinal))
        {
            DetachValidationSource();

            _currentValidationSource = nextValidationSource;
            _currentPropertyName = nextPropertyName;

            if (_currentValidationSource is not null)
            {
                _currentValidationSource.ErrorsChanged += OnErrorsChanged;
            }
        }

        RefreshValidationState();
    }

    private void DetachValidationSource()
    {
        if (_currentValidationSource is not null)
        {
            _currentValidationSource.ErrorsChanged -= OnErrorsChanged;
            _currentValidationSource = null;
        }

        _currentPropertyName = string.Empty;
        ClearValidationState();
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
           || string.Equals(propertyName, _currentPropertyName, StringComparison.Ordinal);

    private void RefreshValidationState()
    {
        if (_currentValidationSource is null || string.IsNullOrWhiteSpace(_currentPropertyName))
        {
            ClearValidationState();
            return;
        }

        string errorMessage = GetFirstErrorMessage(_currentValidationSource.GetErrors(_currentPropertyName));

        if (string.IsNullOrWhiteSpace(errorMessage))
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