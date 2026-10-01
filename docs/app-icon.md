# App icon

Changing the app icon is one command. Drop your art into `assets/app-icon/candidates/` and run:

```powershell
./assets/app-icon/Set-AppIcon.ps1 -Name <candidate> [-PlateColor '#RRGGBB']
```

Then rebuild. `-Name` tab-completes from the `candidates` folder; the template ships one, `uno`.

## What the script does

1. Copies `candidates/<name>.svg` to `src/AppTemplate/Assets/Icons/icon_foreground.svg` and
   `src/AppTemplate/Assets/Splash/splash_screen.svg`, then **sets their timestamps to now**.
   `Copy-Item` keeps the candidate's old timestamp, and both `PreserveNewest` and Resizetizer skip a
   source that looks older than their last output, so you'd keep seeing the previous logo.
2. Writes `Assets/Icons/icon.svg`, the opaque plate behind the art on Android and iOS, in
   `-PlateColor` (white by default).
3. Runs `export-windows-assets.cs` to regenerate the 50 Windows package images in
   `src/AppTemplate/Platforms/Windows/`.

Commit the changed SVGs and PNGs together.

## Windows package images

`export-windows-assets.cs` is a .NET 10 file-based app. You can run it on its own:

```powershell
dotnet run assets/app-icon/export-windows-assets.cs -- <art.svg> <output folder>
```

It renders the app icon (scaled, `targetsize`, `altform-unplated` and `altform-lightunplated`), the
small, medium, wide and large tiles, the splash screen and the package logo, with the Visual Studio
asset generator's names, sizes and padding. It uses **Svg.Skia**, the library Uno's Resizetizer
renders SVGs with, pinned to the version the Uno SDK resolves, so Windows matches the other heads.
`Set-AppIcon.ps1` warns when the Uno.Sdk in `global.json` moves to a different Svg.Skia; bump the
`#:package Svg.Skia@…` line when it does. The SDK's version is the `SvgSkia` group in
`uno.sdk/<version>/targets/netstandard2.0/packages.json` in your NuGet package cache.

The `Directory.Build.props` and `Directory.Packages.props` next to the script keep it out of the
repo's central package management and Nerdbank.GitVersioning, so it can pin its own package.

### Which builds use them

**Prod** Windows builds point the package manifest at these images
(`src/AppTemplate/Platforms/Windows/PackageAssets.targets`). Resizetizer's own Windows images fill
every tile edge to edge, which is what you'd otherwise ship to the Store.

**Dev** builds keep Resizetizer's images, so a build-time Dev badge drawn onto the icon still shows
up on the taskbar and Start menu. Override either way with `-p:UseWindowsPackageAssets=true|false`.

## Rules for the artwork

- **The App Store rejects transparent main icons (ITMS-90717).** That's why iOS (and Android) draw
  the art on the opaque `icon.svg` plate. On iOS the art is scaled to **0.8**
  (`UnoIconForegroundScale` in `AppTemplate.csproj`), so the plate reads as a clear margin rather
  than a stray rim around the edge.
- **No `<use>` elements.** WinUI's native `SvgImageSource` (if you show the logo as an SVG, e.g. in
  the title bar or an About page) then renders it exactly like Skia does.
- **Round art should fill about 99% of the canvas.** A circle reads smaller than the square icons
  next to it on the taskbar, so the usual padding makes it look lost.
- **No outer drop shadow.** With the art that close to the edge the shadow gets clipped flat.
