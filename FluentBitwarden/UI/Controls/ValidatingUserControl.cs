using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace FluentBitwarden.UI.Controls;

public abstract class ValidatingUserControl : UserControl, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _errors.Values
                .SelectMany(static errors => errors)
                .ToArray();
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Array.Empty<string>();
    }

    protected void SetError(string propertyName, string error)
    {
        _errors[propertyName] = [error];
        OnErrorsChanged(propertyName);
    }

    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        var list = errors
            .Where(static error => !string.IsNullOrWhiteSpace(error))
            .ToList();

        if (list.Count == 0)
        {
            ClearError(propertyName);
            return;
        }

        _errors[propertyName] = list;
        OnErrorsChanged(propertyName);
    }

    protected void ClearError(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }

    protected void ClearAllErrors()
    {
        if (_errors.Count == 0)
            return;

        _errors.Clear();
        foreach (var propertyName in _errors.Keys)
        {
            OnErrorsChanged(propertyName);
        }
    }

    protected bool ValidateRequired(string propertyName, string? value, string error)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ClearError(propertyName);
            return true;
        }

        SetError(propertyName, error);
        return false;
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}