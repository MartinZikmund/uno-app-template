namespace AppTemplate.Services.Store;

/// <summary>
/// Store abstraction surfacing the operations a paywall needs: price lookup, entitlement
/// state, purchasing and restoring the "Pro" upgrade.
/// </summary>
public interface IStoreService
{
    /// <summary>
    /// Gets the localized, display-ready price string for the "Pro" offering, if available.
    /// </summary>
    /// <returns>The localized price string, or <see langword="null"/> when it cannot be determined.</returns>
    Task<string?> GetPriceAsync();

    /// <summary>
    /// Gets a value indicating whether the "Pro" entitlement is currently active.
    /// </summary>
    /// <returns><see langword="true"/> when the entitlement is active; otherwise <see langword="false"/>.</returns>
    Task<bool> HasProAsync();

    /// <summary>
    /// Attempts to purchase the "Pro" entitlement.
    /// </summary>
    /// <returns><see langword="true"/> when the entitlement is active after the purchase; otherwise <see langword="false"/>.</returns>
    Task<bool> TryPurchaseProAsync();

    /// <summary>
    /// Attempts to restore previously made purchases for the current user.
    /// </summary>
    /// <returns><see langword="true"/> when the "Pro" entitlement is active after restoring; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRestorePurchasesAsync();
}
