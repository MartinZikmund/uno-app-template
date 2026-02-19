using Microsoft.Extensions.Localization;

namespace AppTemplate.Services.Localization;

/// <summary>
/// Static facade for easy localization access throughout the app.
/// Initialize with Localizer.Initialize(stringLocalizer) at app startup.
/// </summary>
public static class Localizer
{
    private static IStringLocalizer? _localizer;

    /// <summary>
    /// Initializes the localizer with the provided string localizer instance.
    /// </summary>
    public static void Initialize(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Gets a localized string by key.
    /// Returns ???Key??? if not found or localizer not initialized.
    /// </summary>
    public static string Get(string key)
    {
        if (_localizer is null)
        {
            return $"???{key}???";
        }

        var value = _localizer[key];
        return value.ResourceNotFound ? $"???{key}???" : value.Value;
    }

    /// <summary>
    /// Gets a localized string by key with format arguments.
    /// </summary>
    public static string Get(string key, params object[] arguments)
    {
        if (_localizer is null)
        {
            return $"???{key}???";
        }

        var value = _localizer[key, arguments];
        return value.ResourceNotFound ? $"???{key}???" : value.Value;
    }

    /// <summary>
    /// Tries to get a localized string by key.
    /// Returns null if not found.
    /// </summary>
    public static string? TryGet(string key)
    {
        if (_localizer is null)
        {
            return null;
        }

        var value = _localizer[key];
        return value.ResourceNotFound ? null : value.Value;
    }
}
