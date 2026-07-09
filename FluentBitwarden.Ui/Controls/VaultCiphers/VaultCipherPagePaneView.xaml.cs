using System.Collections.ObjectModel;
using FluentBitwarden.Contracts.Modules.Vault;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("SearchText")]
[DependencyProperty<VaultCipher>("SelectedCipher")]
[DependencyProperty<ObservableCollection<VaultCipher>>("FilteredCiphers", DefaultValueExpression = "new System.Collections.ObjectModel.ObservableCollection<VaultCipher>()")]
[DependencyProperty<bool>("IsSearchFieldOpen")]
[DependencyProperty<VaultCipherQuery>("Query")]
[DependencyProperty<CipherId>("RequestedCipherId")]
public sealed partial class VaultCipherPagePaneView : UserControl
{
    public static readonly DependencyProperty SelectedCipherTypeProperty = DependencyProperty.Register(
        nameof(SelectedCipherType),
        typeof(VaultCipherType?),
        typeof(VaultCipherPagePaneView),
        new PropertyMetadata(null, OnSelectedCipherTypePropertyChanged));

    public static readonly DependencyProperty CipherSortFieldProperty = DependencyProperty.Register(
        nameof(CipherSortField),
        typeof(VaultCipherSortField),
        typeof(VaultCipherPagePaneView),
        new PropertyMetadata(VaultCipherSortField.Name, OnCipherSortFieldPropertyChanged));

    public static readonly DependencyProperty CipherSortDirectionProperty = DependencyProperty.Register(
        nameof(CipherSortDirection),
        typeof(VaultCipherSortDirection),
        typeof(VaultCipherPagePaneView),
        new PropertyMetadata(VaultCipherSortDirection.Ascending, OnCipherSortDirectionPropertyChanged));

    private static void OnSelectedCipherTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VaultCipherPagePaneView)d).RaiseFiltersChanged();

    private static void OnCipherSortFieldPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VaultCipherPagePaneView)d).RaiseFiltersChanged();

    private static void OnCipherSortDirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VaultCipherPagePaneView)d).RaiseFiltersChanged();

    public VaultCipherType? SelectedCipherType
    {
        get => (VaultCipherType?)GetValue(SelectedCipherTypeProperty);
        set
        {
            if (Equals(value, SelectedCipherType))
                return;

            SetValue(SelectedCipherTypeProperty, value);
        }
    }

    public VaultCipherSortField CipherSortField
    {
        get => (VaultCipherSortField)GetValue(CipherSortFieldProperty);
        set
        {
            if (Equals(value, CipherSortField))
                return;

            SetValue(CipherSortFieldProperty, value);
        }
    }

    public VaultCipherSortDirection CipherSortDirection
    {
        get => (VaultCipherSortDirection)GetValue(CipherSortDirectionProperty);
        set
        {
            if (Equals(value, CipherSortDirection))
                return;

            SetValue(CipherSortDirectionProperty, value);
        }
    }

    private readonly IVaultClient _vaultClient;

    private bool _isApplyingQuery;
    private CancellationTokenSource? _queryCancellationTokenSource;
    private int _queryRequestId;

    public VaultCipherPagePaneView()
    {
        InitializeComponent();

        _vaultClient = App.Current.GetRequiredService<IVaultClient>();
    }

    private void VaultCipherPagePaneView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        CancelPendingQuery();
    }

    partial void OnRequestedCipherIdChanged(CipherId newValue)
    {
        _ = RunQueryAsync(BuildQuery());
    }

    partial void OnQueryChanged(VaultCipherQuery? newValue)
    {
        if (newValue is null || _isApplyingQuery)
            return;

        _isApplyingQuery = true;
        try
        {
            SearchText = newValue.SearchText;
            SelectedCipherType = newValue.CipherType;
        }
        finally
        {
            _isApplyingQuery = false;
        }

        _ = RunQueryAsync(BuildQuery());
    }

    partial void OnSearchTextChanged(string? newValue)
    {
        if (!string.IsNullOrWhiteSpace(newValue))
            IsSearchFieldOpen = true;

        RaiseFiltersChanged();
    }

    private void RaiseFiltersChanged()
    {
        if (_isApplyingQuery)
            return;

        var query = BuildQuery();

        _isApplyingQuery = true;
        try
        {
            Query = query;
        }
        finally
        {
            _isApplyingQuery = false;
        }

        _ = RunQueryAsync(query);
    }

    private VaultCipherQuery BuildQuery() => new()
    {
        SearchText = SearchText ?? string.Empty,
        CipherType = SelectedCipherType,
        SortField = CipherSortField,
        SortDirection = CipherSortDirection
    };

    private async Task RunQueryAsync(VaultCipherQuery query)
    {
        CancelPendingQuery();

        CancellationTokenSource cts = new();
        _queryCancellationTokenSource = cts;
        int requestId = ++_queryRequestId;

        try
        {
            var ciphers = await _vaultClient.SearchCiphersAsync(query, cts.Token);

            if (requestId != _queryRequestId)
                return;

            FilteredCiphers.ReplaceWith(ciphers);

            if (RequestedCipherId != CipherId.Empty)
            {
                SelectedCipher = FilteredCiphers.FirstOrDefault(c => c.Id == RequestedCipherId);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
        }
        finally
        {
            if (ReferenceEquals(_queryCancellationTokenSource, cts))
                _queryCancellationTokenSource = null;

            cts.Dispose();
        }
    }

    private void CancelPendingQuery()
    {
        _queryCancellationTokenSource?.Cancel();
        _queryCancellationTokenSource?.Dispose();
        _queryCancellationTokenSource = null;
    }
}
