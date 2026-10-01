# Theming

You want the app to look like *your* app: your colours, your type, and still readable in Light, Dark and
the Windows contrast themes. Everything lives in two resource dictionaries, merged in `App.xaml`:

| I want to change… | Edit |
| --- | --- |
| The brand colour (and every accent-coloured stock control with it) | `AppBrandColor*` ramp at the top of `src/AppTemplate/Resources/Colors.xaml` |
| A specific semantic colour (brand text, surfaces, overlay) | The matching `App*Brush` in **all three** theme dictionaries in `Colors.xaml` |
| Follow the user's Windows accent instead of a brand colour | Delete the `SystemAccentColor*` block in `Colors.xaml` |
| Heading font | `AppDisplayFontFamily` in `src/AppTemplate/Resources/Typography.xaml` |
| Body font for the whole app | Override `ContentControlThemeFontFamily` in `Typography.xaml` (commented example there) |
| Text sizes / weights | Nothing - the `App*TextBlockStyle`s inherit the WinUI type ramp; override a setter only if you must |

## Colour tokens

`Colors.xaml` has three layers:

1. **Brand ramp** - `AppBrandColor` plus `Light1-3` and `Dark1-3`. This is the palette; pick seven shades of
   one hue. `Dark1` must reach 4.5:1 against a white page and `Light2` against a black one, because that's
   where Light and Dark theme put accent text and fills.
2. **Accent override** - `SystemAccentColor` and its six variants point at the ramp. WinUI builds every
   accent brush (`AccentFillColorDefaultBrush`, selection indicators, toggles, focus) from these, so stock
   controls pick up the brand without being restyled.
3. **Semantic tokens** - `ThemeDictionaries` with `Light`, `Dark` and `HighContrast`:

   | Token | Light / Dark | High Contrast |
   | --- | --- | --- |
   | `AppBrandBrush` | brand fill (`Dark1` / `Light2`) | `SystemColorHighlightColor` |
   | `AppOnBrandBrush` | text on a brand fill | `SystemColorHighlightTextColor` |
   | `AppBrandTextBrush` | brand-coloured text on the page (`Dark2` / `Light3`) | `SystemColorWindowTextColor` |
   | `AppSurfaceBrush` | card background | `SystemColorWindowColor` |
   | `AppSurfaceStrokeBrush` | card border | `SystemColorWindowTextColor` |
   | `AppTextSecondaryBrush` | secondary text | `SystemColorWindowTextColor` |
   | `AppOverlayBackgroundBrush` | translucent scrim | `SystemColorWindowColor` |
   | `AppSurfaceBorderThickness` | `1` | `2` |

Use them with `{ThemeResource}` so they update when the theme flips:

```xml
<FontIcon Foreground="{ThemeResource AppBrandBrush}" Glyph="&#xE80F;" />
```

### High Contrast

The `HighContrast` dictionary may only use the user's `SystemColor*` colours, in the documented pairs
(window/window text, highlight/highlight text, button face/button text). No brand colours, no accent, no
opacity - the user picked those colours for a reason. Secondary text maps to `SystemColorWindowTextColor`,
not `SystemColorGrayTextColor`, which is reserved for disabled content.

`App.xaml.cs` sets `HighContrastAdjustment = None` on the WinUI head. Without it WinUI repaints text and
icons in its own contrast colours and ignores this dictionary (Uno doesn't do that adjustment, so the
line is Windows-only). On Windows, the title bar caption buttons also hand their colours back to the
system while a contrast theme is on (`ThemeManager`).

To check it: **Settings → Accessibility → Contrast themes**, pick one (Aquatic and Desert cover dark and
light), and look at the running app. Turn it off again afterwards.

## Typography

`Typography.xaml` defines `AppDisplay`, `AppTitleLarge`, `AppTitle`, `AppSubtitle`, `AppBodyStrong`, `AppBody`
and `AppCaption` `TextBlockStyle`s. Each is `BasedOn` the WinUI style of the same name, so sizes and
weights follow the Fluent ramp; the app only picks the family.

```xml
<TextBlock Style="{StaticResource AppTitleLargeTextBlockStyle}" Text="{x:Bind ViewModel.PageTitle}" />
```

### Bundled font

Headings use **Outfit** (SIL Open Font License 1.1, licence in `Assets/Fonts/Outfit-OFL.txt`) to show the
pattern. To swap it:

1. Drop your font into `src/AppTemplate/Assets/Fonts/` with its licence file.
2. Point `AppDisplayFontFamily` at it: `ms-appx:///Assets/Fonts/MyFont.ttf#My Font Family`. The part after
   `#` is the family name inside the font file, not the file name.
3. If it's a **variable** font, add a `MyFont.ttf.manifest` listing a static file per weight you use (see
   `Outfit.ttf.manifest`). Windows, Android and WebAssembly read the variable axes; the Skia desktop and iOS
   heads can't yet and would otherwise render the font's default instance - for Outfit that's Thin. See
   [Uno custom fonts](https://platform.uno/docs/articles/features/custom-fonts.html#variable-fonts-and-font-manifest).

Not bundling a font at all? Delete the `Assets/Fonts` folder and redirect the key the same way `AppBodyFontFamily`
does: `<StaticResource x:Key="AppDisplayFontFamily" ResourceKey="ContentControlThemeFontFamily" />`.

## Rules

The no-hardcoded-colours rule for contributors and agents is in
[`.claude/rules/theming.md`](../.claude/rules/theming.md).
