# CommunityToolkit source copy

Why `SettingsCard` and the converters build from `src/AppTemplate/ThirdParty/CommunityToolkit/` instead of the
`CommunityToolkit.WinUI.*` packages, and how to switch back.

## Why

Every published `CommunityToolkit.WinUI.*` package is built against Uno 5.5 and references an `Uno` assembly that Uno 7
no longer ships. Referencing one fails the desktop, WebAssembly and iOS heads with `CS0012` on `CoreDispatcher` /
`RadialControllerMenuKnownIcon` in the generated `BindableMetadata.g.cs`. The Windows head builds, but the copy is used
on every head so they behave alike. The upstream fix is in progress in CommunityToolkit/Windows#880.

## What the app uses

`SettingsCard` (`Views/SettingsView.xaml`) and four converters (`Resources/Converters.xaml`). Namespaces stay
`CommunityToolkit.WinUI.Controls` and `CommunityToolkit.WinUI.Converters`, so no consumer changes. The folder's own
[README](../src/AppTemplate/ThirdParty/CommunityToolkit/README.md) lists the exact files, the upstream tag, the local
edits and the removal steps.

## Using more of the toolkit

Copy the extra files from the same upstream tag into that folder and list them in the README. `SettingsExpander` is the
likeliest next one. Prefer waiting for the package if you only need it later.

## Going back to the packages

Follow "Removing the copy" in the folder README: delete the folder, drop the `App.xaml` merge, restore the package
references, drop `src/AppTemplate/TrimmerRoots.xml` and check the trimmed Windows app still launches (see
[windows-packaging.md](./windows-packaging.md)). `CommunityToolkit.Mvvm` is unaffected throughout, it works with Uno 7.
