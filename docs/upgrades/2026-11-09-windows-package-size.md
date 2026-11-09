---
title: Shrink the Windows package and fail CI if it can't launch
impact: recommended
features: []
platforms: [windows]
---

## Why

The x64 publish layout was 405.2 MB, and most of it was a bundled runtime and native libraries the app never loads. With
these changes it's 47.1 MB. Trimming also let a package build green and then crash at launch, so the smoke launch in CI
now fails the build instead of only warning.

The size of each step is in
[windows-packaging.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/windows-packaging.md#package-size).

## Who is affected

Apps with the Windows head. Do [the Uno.Sdk 7 note](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/upgrades/2026-11-09-uno-7.md) first, because these steps
assume WinAppSDK 2.x. Do [the preferences note](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/upgrades/2026-11-09-preferences-source-generated-json.md) too,
because reflection-based JSON is off in a trimmed build.

The package now depends on the Windows App Runtime rather than bundling it. The Store installs it for you, but anyone
sideloading your package needs that runtime installed.

## Steps

`mzuno-app upgrade --patch` delivers these, except where you've changed the same lines. Steps 3 and 4 need the CI workflows.

1. Add this to `src/AppTemplate/AppTemplate.csproj`. If your app uses Windows ML, leave out the two ML prunes. If it draws
   with Skia on Windows, leave out the two SkiaSharp prunes.

   ```xml
   <ItemGroup Condition="'$(IsWinAppSdk)' == 'true'">
       <PrunePackageReference Include="Microsoft.WindowsAppSDK.ML" Version="99.0.0" />
       <PrunePackageReference Include="Microsoft.Windows.AI.MachineLearning" Version="99.0.0" />
       <PrunePackageReference Include="SkiaSharp.NativeAssets.Win32" Version="99.0.0" />
       <PrunePackageReference Include="SkiaSharp.NativeAssets.WinUI" Version="99.0.0" />
   </ItemGroup>

   <PropertyGroup Condition="'$(IsWinAppSdk)' == 'true'">
       <WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
       <TrimMode>partial</TrimMode>
   </PropertyGroup>

   <ItemGroup Condition="'$(IsWinAppSdk)' == 'true'">
       <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
   </ItemGroup>
   ```

2. Create `src/AppTemplate/TrimmerRoots.xml`:

   ```xml
   <linker>
     <assembly fullname="Microsoft.WinUI">
       <namespace fullname="Microsoft.UI.Xaml.Markup" />
       <namespace fullname="ABI.Microsoft.UI.Xaml.Markup" />
     </assembly>
   </linker>
   ```

3. In `.github/workflows/_build-windows.yml`, replace the `Locate the generated manifest` step. Only MSBuild's final
   manifest declares the Windows App Runtime dependency, and `winapp` doesn't add it.

   ```yaml
   - name: Locate the package manifest
     id: manifest
     run: |
       set -euo pipefail
       IFS=, read -ra ARCHES <<< "${{ inputs.architectures }}"
       first="${ARCHES[0]}"
       # MSBuild's final AppxManifest.xml, not Uno's generated Package.appxmanifest: only the
       # final one declares the Microsoft.WindowsAppRuntime framework dependency, and winapp
       # does not add it. winapp still sets each package's architecture from its executable.
       # The <Platform> segment appears in bin/ for some architectures and not others,
       # so find it rather than assuming the path.
       man="$(find src/AppTemplate/bin -path "*Release/$TFM/win-$first/AppxManifest.xml" 2>/dev/null | head -1 || true)"
       [ -n "$man" ] || { echo "::error::could not find the AppxManifest.xml for win-$first"; exit 1; }
       echo "manifest=$man" >> "$GITHUB_OUTPUT"
       echo "Using $man"
   ```

4. In the same file, replace the `Smoke launch the packaged app` step. It now installs the runtime on the runner and has no
   `continue-on-error`. If your app needs longer than 15 seconds to prove itself, change that here.

   ```yaml
   - name: Smoke launch the packaged app
     if: inputs.smoke_launch && inputs.channel == 'Dev'
     shell: pwsh
     run: |
       $ErrorActionPreference = 'Stop'
       $layout = "src/AppTemplate/bin/Release/$env:TFM/win-x64/publish"
       $manifest = (Get-ChildItem src/AppTemplate/bin -Recurse -Filter AppxManifest.xml |
         Where-Object FullName -match "Release[\\/]$env:TFM[\\/]win-x64[\\/]AppxManifest\.xml$" |
         Select-Object -First 1).FullName
       if (-not $manifest) { throw 'no AppxManifest.xml for win-x64' }

       # The package is framework-dependent and the runner has no Windows App Runtime: install the
       # one this build restored, the way the Store would.
       $ns = @{ d = 'http://schemas.microsoft.com/appx/manifest/foundation/windows10' }
       $runtime = (Select-Xml -Path $manifest -Namespace $ns -XPath '//d:PackageDependency').Node |
         Where-Object Name -like 'Microsoft.WindowsAppRuntime.*'
       # A same-named runtime older than the manifest's MinVersion would not satisfy the dependency.
       $hasRuntime = $runtime -and (Get-AppxPackage -Name $runtime.Name |
         Where-Object { $_.Architecture -eq 'X64' -and [version]$_.Version -ge [version]$runtime.MinVersion })
       if ($runtime -and -not $hasRuntime) {
         $packages = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
         # WinAppSDK 2.x ships the runtime in its own package. Pick the newest one that satisfies MinVersion.
         Add-Type -AssemblyName System.IO.Compression.FileSystem
         $msix = Get-ChildItem (Join-Path $packages 'microsoft.windowsappsdk.runtime') -Recurse -Filter "$($runtime.Name).msix" |
           Where-Object FullName -like '*win10-x64*' |
           ForEach-Object {
             $zip = [IO.Compression.ZipFile]::OpenRead($_.FullName)
             try {
               $reader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
               [pscustomobject]@{ Path = $_.FullName; Version = [version]([xml]$reader.ReadToEnd()).Package.Identity.Version }
             }
             finally { $zip.Dispose() }
           } |
           Where-Object Version -ge ([version]$runtime.MinVersion) | Sort-Object Version | Select-Object -Last 1
         if (-not $msix) { throw "no $($runtime.Name).msix >= $($runtime.MinVersion) in $packages" }
         Add-AppxPackage -Path $msix.Path
       }

       $app = winapp run $layout --manifest $manifest --exe "$env:APP_NAME.exe" --detach --json | ConvertFrom-Json
       if ($LASTEXITCODE -ne 0 -or -not $app.ProcessId) { throw "winapp run failed ($LASTEXITCODE)" }
       try {
         $started = Get-Date
         Start-Sleep -Seconds 15
         if (-not (Get-Process -Id $app.ProcessId -ErrorAction SilentlyContinue)) {
           Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $started.AddSeconds(-5) } -ErrorAction SilentlyContinue |
             Where-Object ProviderName -in 'Application Error', '.NET Runtime' | Select-Object -First 2 |
             ForEach-Object { Write-Host $_.Message }
           Write-Host "::error::the packaged app exited within 15 s of launch"
           exit 1
         }
         winapp ui screenshot -a $app.ProcessId --output (Join-Path $env:RUNNER_TEMP 'stage/smoke.png')
         if ($LASTEXITCODE -ne 0) { Write-Host "::warning::smoke screenshot failed" }
       }
       finally {
         Stop-Process -Id $app.ProcessId -ErrorAction SilentlyContinue
         # --force: winapp run registers a staged copy of the layout, not the manifest's own folder
         winapp unregister --manifest $manifest --force
       }
   ```

## Verify

Publish the Windows head in Release the way `_build-windows.yml` does. The app should launch from that layout, and the
layout should be far smaller than before. Then push to `main`: in `build-main.yml`, the Windows job's smoke launch step
should pass.
