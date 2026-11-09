# CommunityToolkit source copy (temporary)

The `CommunityToolkit.WinUI.*` packages are built against Uno 5.5 and do not compile against Uno 7
(`CS0012` on `CoreDispatcher`). Until the toolkit ships a build for Uno 7, the pieces this app uses are
compiled from source here. Namespaces are unchanged, so no consumer XAML or C# differs.

- Upstream: https://github.com/CommunityToolkit/Windows, tag `v8.2.251219` (MIT, see `LICENSE.md`)
- Tracking: CommunityToolkit/Windows#880

## What was copied

| Folder | From `components/` | Why |
|---|---|---|
| `Converters/` | `Converters/src`: `BoolNegationConverter`, `BoolToObjectConverter`, `BoolToVisibilityConverter`, `EmptyObjectToObjectConverter`, `EmptyStringToObjectConverter`, `StringVisibilityConverter`, `ConverterTools` | `Resources/Converters.xaml` |
| `SettingsControls/SettingsCard/` | `SettingsControls/src/SettingsCard/*` | `toolkit:SettingsCard` in `SettingsView.xaml` |
| `SettingsControls/Helpers/` | `ControlHelper`, `ResourceDictionaryExtensions`, `StyleExtensions` | used by `SettingsCard` |
| `Triggers/` | `Triggers/src`: `ControlSizeTrigger`, `IsEqualStateTrigger`, `IsNullOrEmptyStateTrigger` | used by the `SettingsCard` template |
| `Helpers/` | `Helpers/src/WeakEventListener.cs` | used by `IsNullOrEmptyStateTrigger` |

Not copied: `SettingsExpander`, the other converters and triggers, `CornerRadiusConverter`.

## Local edits

Every `.cs` file is identical to upstream apart from line endings. Only these differ:

- `GlobalUsings.cs` (new): the global usings upstream's build tooling provides.
- `SettingsControls/SettingsCard/SettingsCard.xaml`: reformatted by XamlStyler, which CI enforces.
- `LICENSE.md`: upstream's `License.md`, renamed.

## Merged resources

`App.xaml` merges `SettingsCard.xaml`. With the package, `Themes/Generic.xaml` did this.

## Removing the copy

When a CommunityToolkit build for Uno 7 is out:

1. Delete this folder.
2. Remove the `SettingsCard.xaml` merge from `src/AppTemplate/App.xaml`.
3. Restore in `src/Directory.Packages.props` the `CommunityToolkit.WinUI.Controls.SettingsControls` and
   `CommunityToolkit.WinUI.Converters` versions, and reference both from `src/AppTemplate/AppTemplate.csproj`.
