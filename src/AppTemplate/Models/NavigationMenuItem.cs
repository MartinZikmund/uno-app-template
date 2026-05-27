namespace AppTemplate.Models;

/// <summary>
/// Represents a single navigation menu entry rendered using the
/// <c>NavigationMenuItemTemplate</c> defined in <c>Resources/DataTemplates.xaml</c>.
/// </summary>
public sealed record NavigationMenuItem(string Glyph, string Label);
