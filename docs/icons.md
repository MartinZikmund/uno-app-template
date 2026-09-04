# Icon glyph constants

Icon glyphs live in a single resource dictionary, `src/AppTemplate/Resources/Icons.xaml`,
instead of being scattered as raw `&#xExxx;` literals throughout the XAML. The dictionary is
merged into the app-wide resources in `src/AppTemplate/App.xaml`.

Each entry is an `x:String` keyed by a descriptive name, using glyph codes from the
Segoe Fluent Icons / Segoe MDL2 Assets font:

```xml
<x:String x:Key="ShareIcon">&#xE72D;</x:String>
```

Reference the glyph from a `FontIcon` via `StaticResource`:

```xml
<FontIcon Glyph="{StaticResource ShareIcon}" />
```

For controls that expect an `IconElement` property (such as `NavigationViewItem.Icon` or
`SettingsCard.HeaderIcon`), set it as a property element:

```xml
<NavigationViewItem.Icon>
    <FontIcon Glyph="{StaticResource ShareIcon}" />
</NavigationViewItem.Icon>
```

Centralizing the glyphs keeps icon usage consistent and makes swapping an icon a one-line change.
Add new icons by introducing another keyed `x:String` in `Icons.xaml`.
