using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FluentBitwarden.Shared.Validation;

internal abstract class ObservableValidatorEx : ObservableValidator, INotifyDataErrorInfo
{
    private readonly Dictionary<string, ValidationResult> _manualErrors = new(StringComparer.Ordinal);

    protected ObservableValidatorEx()
    {
        base.ErrorsChanged += (_, e) =>
        {
            RaiseErrorsChanged(e.PropertyName);
            OnPropertyChanged(nameof(HasErrors));
        };
    }

    public new event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public new bool HasErrors => base.HasErrors || _manualErrors.Count > 0;

    bool INotifyDataErrorInfo.HasErrors => HasErrors;

    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => ErrorsChanged += value;
        remove => ErrorsChanged -= value;
    }

    public new IEnumerable GetErrors(string? propertyName = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return GetAllErrors();

        ValidationResult? error = GetFirstError(propertyName);

        return error is null
            ? Array.Empty<ValidationResult>()
            : new[] { error };
    }

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

    protected void SetError(string propertyName, string errorMessage)
    {
        bool hadErrors = HasErrors;

        _manualErrors[propertyName] = new ValidationResult(
            errorMessage,
            [propertyName]);

        RaiseErrorsChanged(propertyName);

        if (hadErrors != HasErrors)
            OnPropertyChanged(nameof(HasErrors));
    }

    protected void ClearError(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (!_manualErrors.Remove(propertyName))
            return;

        RaiseErrorsChanged(propertyName);

        if (!HasErrors)
            OnPropertyChanged(nameof(HasErrors));
    }

    protected void ClearAllManualErrors()
    {
        if (_manualErrors.Count == 0)
            return;

        _manualErrors.Clear();

        /*var propertyNames = _manualErrors.Keys.ToList();
        _manualErrors.Clear();

        foreach (var propertyName in propertyNames)
            RaiseErrorsChanged(propertyName);*/

        OnPropertyChanged(nameof(HasErrors));
    }

    private ValidationResult? GetFirstError(string propertyName)
    {
        var error = base.GetErrors(propertyName).FirstOrDefault();
        return error ?? _manualErrors.GetValueOrDefault(propertyName);
    }

    private IEnumerable GetAllErrors()
    {
        HashSet<string> emittedProperties = new(StringComparer.Ordinal);

        foreach (var error in base.GetErrors())
        {
            string? propertyName = error.MemberNames.FirstOrDefault();

            if (propertyName is not null)
                emittedProperties.Add(propertyName);

            yield return error;
        }

        foreach ((string propertyName, ValidationResult error) in _manualErrors)
        {
            if (emittedProperties.Contains(propertyName))
                continue;

            yield return error;
        }
    }

    private void RaiseErrorsChanged(string? propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}