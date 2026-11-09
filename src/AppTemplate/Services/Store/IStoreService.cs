namespace AppTemplate.Services.Store;

/// <summary>
/// Abstraction over in-app purchase / store interactions that back the upgrade-to-pro
/// (paywall) experience. Implementations talk to the underlying store (Microsoft Store,
/// App Store, Google Play, ...) and are selected per platform via dependency injection.
/// </summary>
public interface IStoreService
{
    /// <summary>
    /// Gets the localized, formatted price string for the Pro upgrade (for example <c>"$4.99"</c>).
    /// </summary>
    /// <returns>The formatted price, or <see langword="null"/> if it could not be retrieved.</returns>
    Task<string?> GetPriceAsync();

    /// <summary>
    /// Gets a value indicating whether the user already owns the Pro upgrade.
    /// </summary>
    /// <returns><see langword="true"/> if the Pro upgrade is owned; otherwise <see langword="false"/>.</returns>
    Task<bool> HasProAsync();

    /// <summary>
    /// Attempts to purchase the Pro upgrade.
    /// </summary>
    /// <returns><see langword="true"/> if the user now owns the Pro upgrade; otherwise <see langword="false"/>.</returns>
    Task<bool> TryPurchaseProAsync();

    /// <summary>
    /// Attempts to restore previously made purchases (for example after a reinstall or on a new device).
    /// </summary>
    /// <returns><see langword="true"/> if an owned purchase was restored; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRestorePurchasesAsync();
}
