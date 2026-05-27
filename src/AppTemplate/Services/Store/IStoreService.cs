namespace AppTemplate.Services.Store;

/// <summary>
/// Minimal store abstraction surfacing the operations a paywall needs (purchase, restore, price lookup).
/// </summary>
/// <remarks>
/// This is a temporary, local placeholder. The shared toolkit
/// <c>MZikmund.Toolkit.WinUI</c> (currently <c>0.1.13-dev.43</c>) does NOT yet expose an
/// <c>IStoreService</c>. Once the toolkit ships one, delete this file and switch the
/// implementations/registrations over to the toolkit's interface.
/// </remarks>
public interface IStoreService
{
    /// <summary>
    /// Attempts to purchase the application's "Pro" entitlement.
    /// </summary>
    /// <returns><see langword="true"/> when the entitlement is active after the purchase; otherwise <see langword="false"/>.</returns>
    Task<bool> TryPurchaseProAsync();

    /// <summary>
    /// Attempts to restore previously made purchases for the current user.
    /// </summary>
    /// <returns><see langword="true"/> when the "Pro" entitlement is active after restoring; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRestorePurchasesAsync();

    /// <summary>
    /// Gets the localized, display-ready price string for the "Pro" offering, if available.
    /// </summary>
    /// <returns>The localized price string, or <see langword="null"/> when it cannot be determined.</returns>
    Task<string?> GetPriceAsync();
}
