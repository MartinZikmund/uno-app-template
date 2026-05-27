using System.Collections.ObjectModel;
using AppTemplate.Core.ViewModels;
using AppTemplate.Services.Store;

namespace AppTemplate.ViewModels;

public partial class GetProViewModel : ViewModelBase
{
    private readonly IStringLocalizer _localizer;
    private readonly IStoreService _storeService;

    public GetProViewModel(
        IStringLocalizer localizer,
        IStoreService storeService)
    {
        _localizer = localizer;
        _storeService = storeService;
        PageTitle = _localizer["GetProTitle"];

        // Glyphs are Segoe Fluent Icons code points. Replace this sample list with the
        // features your app actually unlocks behind the Pro upgrade.
        Features = new ObservableCollection<ProFeature>
        {
            new("", _localizer["GetProFeatureNoAdsTitle"], _localizer["GetProFeatureNoAdsDescription"]),
            new("", _localizer["GetProFeatureThemesTitle"], _localizer["GetProFeatureThemesDescription"]),
            new("", _localizer["GetProFeatureSyncTitle"], _localizer["GetProFeatureSyncDescription"]),
        };
    }

    /// <summary>
    /// Feature list rendered via a <c>DataTemplate</c> so consuming apps can swap the content.
    /// </summary>
    public ObservableCollection<ProFeature> Features { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsPro { get; set; }

    [ObservableProperty]
    public partial string? Price { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public override async void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        await LoadPriceAsync();
    }

    private async Task LoadPriceAsync()
    {
        try
        {
            ClearError();
            IsBusy = true;
            Price = await _storeService.GetPriceAsync();
        }
        catch (Exception)
        {
            SetError(_localizer["GetProPriceError"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BuyProAsync()
    {
        try
        {
            ClearError();
            IsBusy = true;
            IsPro = await _storeService.TryPurchaseProAsync();
            if (!IsPro)
            {
                SetError(_localizer["GetProPurchaseError"]);
            }
        }
        catch (Exception)
        {
            SetError(_localizer["GetProPurchaseError"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestorePurchasesAsync()
    {
        try
        {
            ClearError();
            IsBusy = true;
            IsPro = await _storeService.TryRestorePurchasesAsync();
            if (!IsPro)
            {
                SetError(_localizer["GetProRestoreError"]);
            }
        }
        catch (Exception)
        {
            SetError(_localizer["GetProRestoreError"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = null;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
