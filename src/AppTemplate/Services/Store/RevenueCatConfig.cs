namespace AppTemplate.Services.Store;

/// <summary>
/// Strongly-typed configuration for the RevenueCat-backed store implementation.
/// </summary>
/// <remarks>
/// Bound from the <c>RevenueCat</c> section of <c>appsettings.json</c> via
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>. This is a plain POCO so it
/// compiles on every target framework (including the desktop build, where the RevenueCat
/// implementation itself is excluded). The default values are obvious placeholders and MUST be
/// replaced with the real keys from the RevenueCat dashboard before shipping.
/// </remarks>
public sealed class RevenueCatConfig
{
    /// <summary>
    /// Gets or sets the RevenueCat public SDK API key used on iOS.
    /// </summary>
    public string IosApiKey { get; set; } = "REPLACE_WITH_IOS_API_KEY";

    /// <summary>
    /// Gets or sets the RevenueCat public SDK API key used on Android.
    /// </summary>
    public string AndroidApiKey { get; set; } = "REPLACE_WITH_ANDROID_API_KEY";

    /// <summary>
    /// Gets or sets the identifier of the entitlement that unlocks the "Pro" features.
    /// </summary>
    public string EntitlementId { get; set; } = "pro";

    /// <summary>
    /// Gets or sets the identifier of the RevenueCat offering presented on the paywall.
    /// When empty, the current (default) offering is used.
    /// </summary>
    public string OfferingIdentifier { get; set; } = "default";
}
