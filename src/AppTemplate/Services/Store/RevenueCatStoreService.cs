#if __IOS__ || __ANDROID__
using Microsoft.Extensions.Options;
using Uno.RevenueCat.Services;

namespace AppTemplate.Services.Store;

/// <summary>
/// RevenueCat-backed <see cref="IStoreService"/> for the mobile (iOS / Android) targets.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is compiled ONLY for iOS and Android — it is excluded from the desktop,
/// Windows, and WASM builds via the surrounding <c>#if __IOS__ || __ANDROID__</c>.
/// </para>
/// <para>
/// It is written against <c>Uno.RevenueCat.Services.IRevenueCatBilling</c> (the abstraction
/// referenced by issue #23). At the time of writing, no <c>Uno.RevenueCat</c> package is published
/// on any configured NuGet feed, so its <see cref="PackageReference"/> in
/// <c>AppTemplate.csproj</c> / <c>Directory.Packages.props</c> is committed in a commented-out,
/// iOS/Android-only form. To finish the integration on a mobile build agent:
/// </para>
/// <list type="number">
/// <item>Add the real <c>Uno.RevenueCat</c> package version once it ships (or adjust the
/// namespace/method names to whichever billing abstraction is adopted).</item>
/// <item>Uncomment the package reference and the <c>AddRevenueCatBilling()</c> registration in
/// <c>App.xaml.cs</c>.</item>
/// <item>Verify the member names used below against the shipped API and adjust as needed.</item>
/// </list>
/// <para>
/// The member access below (offerings, packages, localized price, entitlement state) mirrors the
/// shape of the established community RevenueCat billing abstraction and is a best-effort
/// placeholder until the real package is available.
/// </para>
/// </remarks>
public sealed class RevenueCatStoreService : IStoreService
{
    private readonly IRevenueCatBilling _billing;
    private readonly RevenueCatConfig _config;
    private bool _isInitialized;

    public RevenueCatStoreService(IRevenueCatBilling billing, IOptions<RevenueCatConfig> config)
    {
        _billing = billing;
        _config = config.Value;
    }

    /// <inheritdoc />
    public async Task<bool> TryPurchaseProAsync()
    {
        EnsureInitialized();

        var package = await GetProPackageAsync();
        if (package is null)
        {
            return false;
        }

        var result = await _billing.PurchaseProduct(package);
        return result.IsSuccess && IsProEntitlementActive(result.CustomerInfo);
    }

    /// <inheritdoc />
    public async Task<bool> TryRestorePurchasesAsync()
    {
        EnsureInitialized();

        var customerInfo = await _billing.RestoreTransactions();
        return IsProEntitlementActive(customerInfo);
    }

    /// <inheritdoc />
    public async Task<string?> GetPriceAsync()
    {
        EnsureInitialized();

        var package = await GetProPackageAsync();
        return package?.Product.Pricing.PriceLocalized;
    }

    private async Task<PackageDto?> GetProPackageAsync()
    {
        var offerings = await _billing.GetOfferings();
        var offering = string.IsNullOrEmpty(_config.OfferingIdentifier)
            ? offerings.FirstOrDefault()
            : offerings.FirstOrDefault(o => o.Identifier == _config.OfferingIdentifier);

        return offering?.AvailablePackages.FirstOrDefault();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

#if __IOS__
        _billing.Initialize(_config.IosApiKey);
#else
        _billing.Initialize(_config.AndroidApiKey);
#endif
        _isInitialized = true;
    }

    private bool IsProEntitlementActive(CustomerInfoDto? customerInfo) =>
        customerInfo?.Entitlements.Any(e => e.Identifier == _config.EntitlementId && e.IsActive) == true;
}
#endif
