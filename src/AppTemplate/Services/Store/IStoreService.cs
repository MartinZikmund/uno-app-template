namespace AppTemplate.Services.Store;

/// <summary>
/// Abstraction over in-app purchase / store interactions used by the upgrade-to-pro
/// (paywall) experience.
/// </summary>
/// <remarks>
/// This is a minimal <b>placeholder</b> contract that exists so the <c>GetPro</c> scaffold
/// can compile and be wired through dependency injection. It is intended to be replaced by
/// the <c>IStoreService</c> from <c>MZikmund.Toolkit.WinUI</c> once that toolkit exposes one
/// (it does not at the time of writing, v0.1.13-dev.43). When migrating, keep the member
/// shapes aligned with the toolkit implementation or adjust the call sites in
/// <see cref="AppTemplate.ViewModels.GetProViewModel"/>.
/// </remarks>
public interface IStoreService
{
    /// <summary>
    /// Attempts to purchase the "Pro" upgrade.
    /// </summary>
    /// <returns><see langword="true"/> if the user now owns the Pro upgrade; otherwise <see langword="false"/>.</returns>
    Task<bool> TryPurchaseProAsync();

    /// <summary>
    /// Attempts to restore previously made purchases (e.g. after a reinstall or on a new device).
    /// </summary>
    /// <returns><see langword="true"/> if any owned purchase was restored; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRestorePurchasesAsync();

    /// <summary>
    /// Gets the localized, formatted price string for the Pro upgrade (for example <c>"$4.99"</c>).
    /// </summary>
    /// <returns>The formatted price, or <see langword="null"/> if it could not be retrieved.</returns>
    Task<string?> GetPriceAsync();
}
