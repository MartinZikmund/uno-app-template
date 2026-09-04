#if __IOS__ || __ANDROID__
using AppTemplate.Services.Dialogs;
using Microsoft.Extensions.Options;
using Uno.RevenueCat.Enums;
using Uno.RevenueCat.Models;
using Uno.RevenueCat.Services;

namespace AppTemplate.Services.Store;

/// <summary>
/// RevenueCat-backed <see cref="IStoreService"/> for the mobile (iOS / Android) targets.
/// </summary>
/// <remarks>
/// Compiled only for iOS and Android via the surrounding <c>#if __IOS__ || __ANDROID__</c>; the
/// desktop, Windows and WASM heads use other <see cref="IStoreService"/> implementations. It is
/// backed by <c>MZikmund.Uno.RevenueCat</c> (registered with <c>services.AddRevenueCat()</c>) and
/// reads its API keys, entitlement and product identifiers from <see cref="RevenueCatConfig"/>.
/// </remarks>
public class RevenueCatStoreService : IStoreService
{
    private readonly IRevenueCatBilling _billing;
    private readonly IDialogService _dialogService;
    private readonly RevenueCatConfig _options;
    private readonly Lock _initializationLock = new();
    private bool? _hasPro;
    private bool _isInitialized;

    public RevenueCatStoreService(
        IRevenueCatBilling billing,
        IDialogService dialogService,
        IOptions<RevenueCatConfig> options)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private string ProProductId =>
#if __IOS__
        _options.IOSProProductId;
#else
        _options.AndroidProProductId;
#endif

    public async Task<string?> GetPriceAsync()
    {
        try
        {
            EnsureInitialized();

            var package = await GetProPackageAsync();
            return package?.Product.Pricing?.PriceLocalized;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting price: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> HasProAsync()
    {
        if (_hasPro.HasValue)
        {
            return _hasPro.Value;
        }

        try
        {
            EnsureInitialized();

            var customerInfo = await _billing.GetCustomerInfoAsync();
            if (customerInfo is not null)
            {
                var proEntitlement = customerInfo.Entitlements
                    .FirstOrDefault(e => e.Identifier == _options.EntitlementId);
                _hasPro = proEntitlement?.IsActive ?? false;
                return _hasPro.Value;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking pro status: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> TryPurchaseProAsync()
    {
        try
        {
            EnsureInitialized();

            var package = await GetProPackageAsync();
            if (package is null)
            {
                await ShowErrorAsync("The Pro product could not be found.");
                return false;
            }

            var purchaseResult = await _billing.PurchaseProductAsync(package);
            if (purchaseResult.IsSuccess)
            {
                _hasPro = true;
                return true;
            }

            switch (purchaseResult.ErrorStatus)
            {
                case PurchaseErrorStatus.PurchaseCancelledError:
                    // User cancelled, no error message needed.
                    return false;

                case PurchaseErrorStatus.ProductAlreadyPurchasedError:
                    _hasPro = true;
                    return true;

                case PurchaseErrorStatus.NetworkError:
                    await ShowErrorAsync("A network error occurred. Please try again.");
                    break;

                case PurchaseErrorStatus.StoreProblemError:
                case PurchaseErrorStatus.InvalidReceiptError:
                    await ShowErrorAsync("The store reported a problem. Please try again later.");
                    break;

                default:
                    await ShowErrorAsync($"The purchase could not be completed. {purchaseResult.ErrorStatus}");
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"The purchase could not be completed. {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TryRestorePurchasesAsync()
    {
        try
        {
            EnsureInitialized();

            var customerInfo = await _billing.RestoreTransactionsAsync();
            var proEntitlement = customerInfo?.Entitlements
                .FirstOrDefault(e => e.Identifier == _options.EntitlementId);

            // Reflect the restored state even when no active entitlement was found, so a stale
            // "true" from an earlier call doesn't linger.
            _hasPro = proEntitlement?.IsActive ?? false;

            if (_hasPro == true)
            {
                return true;
            }

            await _dialogService.ShowAsync("Restore purchases", "No purchases were found to restore.");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring purchases: {ex.Message}");
            await ShowErrorAsync($"Purchases could not be restored. {ex.Message}");
            return false;
        }
    }

    private async Task<PackageDto?> GetProPackageAsync()
    {
        var offerings = await _billing.GetOfferingsAsync();
        var currentOffering = offerings.FirstOrDefault(o => o.IsCurrent);
        return currentOffering?.AvailablePackages
            .FirstOrDefault(p => p.Product.Sku == ProProductId);
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        // Double-checked locking: GetPriceAsync/TryPurchaseProAsync/TryRestorePurchasesAsync can
        // race on the first call, and _billing.Initialize must only run once.
        lock (_initializationLock)
        {
            if (_isInitialized)
            {
                return;
            }

#if __IOS__
            var apiKey = _options.IOSApiKey;
#else
            var apiKey = _options.AndroidApiKey;
#endif
            _billing.Initialize(apiKey);
            _isInitialized = true;
        }
    }

    private async Task ShowErrorAsync(string message) =>
        await _dialogService.ShowAsync("Store error", message);
}
#endif
