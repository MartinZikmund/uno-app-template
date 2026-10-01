# Theming & colours

## No hardcoded colours in XAML
- **Never** write a colour literal on an element: no `Foreground="Black"`, `Background="White"`,
  `Fill="Gold"`, `BorderBrush="#FF0000"`. A literal looks right in one theme and is invisible in the other,
  and it ignores the user's High Contrast colours entirely.
- Use a **theme resource** instead, always via `{ThemeResource}` at the usage site so it updates when the
  theme changes:
  - an app token from `src/AppTemplate/Resources/Colors.xaml` (`AppBrandBrush`, `AppTextSecondaryBrush`, …), or
  - a stock WinUI brush (`TextFillColorSecondaryBrush`, `CardBackgroundFillColorDefaultBrush`, …).
- `Transparent` is the one allowed literal - it has no colour to get wrong.
- Need a colour that doesn't exist yet? Add a token to `Colors.xaml` in **all three** theme dictionaries
  (`Light`, `Dark`, `HighContrast`), same key order in each. Never use `x:Key="Default"`.

## Theme dictionaries
- Light/Dark: real colours, or `<StaticResource ResourceKey="…Brush" />` redirects to WinUI brushes.
- HighContrast: **only** `SystemColor*` colours (`<SolidColorBrush Color="{ThemeResource SystemColorWindowTextColor}" />`),
  in their documented foreground/background pairs. No brand colours, no accent, no opacity.
- The brand ramp drives `SystemAccentColor*`. Change the palette there, not by restyling controls.

## Typography
- Text styles come from `src/AppTemplate/Resources/Typography.xaml` (`AppTitleTextBlockStyle`, …) or the
  stock WinUI ramp. Don't set raw `FontSize`/`FontFamily` on individual TextBlocks.

See [docs/theming.md](../../docs/theming.md) for where to change what.
