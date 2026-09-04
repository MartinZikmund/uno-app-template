namespace AppTemplate.Models;

/// <summary>
/// Represents a single feature unlocked by the "Pro" upgrade, rendered in the
/// <c>GetPro</c> paywall feature list via a <c>DataTemplate</c>.
/// </summary>
/// <param name="Glyph">A Segoe Fluent Icons glyph shown next to the feature.</param>
/// <param name="Title">Short, localized feature title.</param>
/// <param name="Description">Optional longer, localized feature description.</param>
public record ProFeature(string Glyph, string Title, string? Description = null);
