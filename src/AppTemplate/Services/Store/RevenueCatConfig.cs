namespace AppTemplate.Services.Store;

/// <summary>
/// Strongly-typed configuration for the RevenueCat-backed store implementation.
/// </summary>
/// <remarks>
/// Bound from the <c>RevenueCatConfig</c> section of <c>appsettings.json</c> via
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>. It is a plain record so it
/// compiles on every target framework (including the desktop build, where the RevenueCat
/// implementation itself is excluded). The default values are obvious placeholders and must be
/// replaced with the real keys and product identifiers from the RevenueCat dashboard before
/// shipping.
/// </remarks>
public record RevenueCatConfig
{
    /// <summary>
    /// Gets the RevenueCat public SDK API key used on iOS.
    /// </summary>
    public string IOSApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the RevenueCat public SDK API key used on Android.
    /// </summary>
    public string AndroidApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the identifier of the entitlement that unlocks the "Pro" features.
    /// </summary>
    public string EntitlementId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the store product identifier (SKU) of the "Pro" purchase on iOS.
    /// </summary>
    public string IOSProProductId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the store product identifier (SKU) of the "Pro" purchase on Android.
    /// </summary>
    public string AndroidProProductId { get; init; } = string.Empty;
}
