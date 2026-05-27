# Windows (WinAppSDK) head

On the `net10.0-windows10.0.26100` target framework the app is compiled as a plain
WinUI / Windows App SDK application using Microsoft's own tooling (Uno Platform is not
involved at runtime on this head). See
[How Uno Platform Works](https://platform.uno/docs/articles/how-uno-works.html#windows-using-winappsdk).

## Running from the command line

### Packaged (`dotnet run`)

`dotnet run` support for the packaged WinAppSDK head is provided by the
[`Microsoft.Windows.SDK.BuildTools.WinApp`](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.WinApp)
package (referenced for the Windows target in `AppTemplate.csproj`). Its MSBuild targets
hook into the standard `dotnet run` pipeline: they build the project, create a
loose-layout package, register it with Windows (like a real MSIX install) and launch it.
This mirrors what the new `dotnet new winui` templates support — see
[Introducing dotnet new templates for WinUI](https://devblogs.microsoft.com/ifdef-windows/introducing-dotnet-new-templates-for-winui/).

```powershell
# From the src/AppTemplate folder
dotnet run -f net10.0-windows10.0.26100
```

The project defaults to a packaged build (`WindowsPackageType=MSIX`), so no extra flags
are required. Requirements: Windows 10 or later and Developer Mode enabled (or an elevated
terminal) so the loose-layout package identity can be registered.

Optional MSBuild properties exposed by the package (set on the command line with
`-p:Name=Value` or in the `.csproj`):

| Property | Default | Description |
|----------|---------|-------------|
| `EnableWinAppRunSupport` | `true` | Enable/disable the `dotnet run` integration. |
| `WinAppLaunchArgs` | (empty) | Arguments passed to the app on launch. |
| `WinAppRunUseExecutionAlias` | `false` | Launch via execution alias instead of AUMID activation (keeps console I/O in the current terminal). |
| `WinAppRunNoLaunch` | `false` | Register identity without launching (attach your own debugger afterwards). |
| `WinAppRunDebugOutput` | `false` | Capture `OutputDebugString` and first-chance exceptions. |

### Unpackaged (`dotnet run`)

To run unpackaged, override `WindowsPackageType` so no MSIX identity is created:

```powershell
# From the src/AppTemplate folder
dotnet run -f net10.0-windows10.0.26100 -p:WindowsPackageType=None
```

The matching launch profile is **App Template (WinAppSDK Unpackaged)** in
`Properties/launchSettings.json` (`commandName: Project`).

### Desktop (Skia) head — for comparison

The cross-platform Skia desktop head runs on Windows (and Linux/macOS) and does not use
Windows App SDK:

```powershell
# From the src/AppTemplate folder
dotnet run -f net10.0-desktop
```

## IDE

In Visual Studio / Rider pick one of the Windows launch profiles from the debug target
dropdown:

- **App Template (WinAppSDK Packaged)** — packaged MSIX run (`commandName: MsixPackage`).
- **App Template (WinAppSDK Unpackaged)** — unpackaged run (`commandName: Project`).

> Note: a Visual Studio issue can hide the unpackaged profile when iOS/Android target
> frameworks are present. See the
> [Uno common issues guide](https://platform.uno/docs/articles/common-issues-vs2022.html#unable-to-select-the--myapp-unpackaged-winappsdk--profile)
> if the profile is not selectable.
