namespace AppTemplate.Core.Configuration;

/// <summary>
/// Shared, configuration-bindable application settings.
/// </summary>
/// <remarks>
/// These values are sourced from <c>appsettings.json</c> (and environment-specific
/// overrides such as <c>appsettings.development.json</c>) and bound to the
/// <c>AppConfig</c> section. For release-time constants that are baked into a build
/// (rather than configured per environment), see
/// <see cref="AppTemplate.Core.Infrastructure.ApplicationReleaseInfo"/>.
/// </remarks>
public record AppConfig
{
    /// <summary>
    /// Gets the name of the current environment (for example, <c>Development</c> or <c>Production</c>).
    /// </summary>
    public string? Environment { get; init; }

    /// <summary>
    /// Gets a value indicating whether telemetry collection is enabled. Defaults to <see langword="false"/>.
    /// </summary>
    public bool TelemetryEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the example feature flag is enabled. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Provided as a representative feature flag; rename or replace it with the flags your app needs.
    /// </remarks>
    public bool EnableExampleFeature { get; init; }
}
