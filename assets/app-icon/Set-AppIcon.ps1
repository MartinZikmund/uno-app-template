<#
.SYNOPSIS
Makes one of the candidate icons the app icon: copies its SVG everywhere the app reads it from,
writes the opaque Android/iOS plate and regenerates the Windows package images. Rebuild afterwards.

.EXAMPLE
./assets/app-icon/Set-AppIcon.ps1 -Name uno -PlateColor '#FFFFFF'
#>
param(
    # File name (without .svg) of a candidate in assets/app-icon/candidates.
    [Parameter(Mandatory)]
    [ArgumentCompleter({
        param($command, $parameter, $word)
        Get-ChildItem (Join-Path $PSScriptRoot 'candidates') -Filter "$word*.svg" | ForEach-Object BaseName
    })]
    [string]$Name,

    # Opaque plate behind the art on Android and iOS. The App Store rejects transparent icons (ITMS-90717).
    [string]$PlateColor = '#FFFFFF'
)

$ErrorActionPreference = 'Stop'

$candidates = Join-Path $PSScriptRoot 'candidates'
$art = Join-Path $candidates "$Name.svg"
if (-not (Test-Path $art)) {
    $available = (Get-ChildItem $candidates -Filter '*.svg' | ForEach-Object BaseName) -join ', '
    throw "No candidate '$Name' in $candidates. Available: $available"
}

$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$app = Join-Path $repo 'src\AppTemplate'
$exporter = Join-Path $PSScriptRoot 'export-windows-assets.cs'

# The exporter should render with the Svg.Skia the Uno SDK uses, or Windows won't match the other heads.
$pinned = [regex]::Match((Get-Content $exporter -Raw), '#:package Svg\.Skia@(\S+)').Groups[1].Value
$unoSdk = (Get-Content (Join-Path $repo 'global.json') -Raw | ConvertFrom-Json).'msbuild-sdks'.'Uno.Sdk'
$packages = $env:NUGET_PACKAGES ? $env:NUGET_PACKAGES : (Join-Path $HOME '.nuget\packages')
$sdkPackages = Join-Path $packages "uno.sdk\$($unoSdk.ToLowerInvariant())\targets\netstandard2.0\packages.json"
if (Test-Path $sdkPackages) {
    $sdkSvgSkia = (Get-Content $sdkPackages -Raw | ConvertFrom-Json | Where-Object group -EQ 'SvgSkia').version
    if ($sdkSvgSkia -and $sdkSvgSkia -ne $pinned) {
        Write-Warning "Uno.Sdk $unoSdk uses Svg.Skia $sdkSvgSkia, but export-windows-assets.cs pins $pinned. Update the '#:package' line."
    }
}

foreach ($target in 'Assets\Icons\icon_foreground.svg', 'Assets\Splash\splash_screen.svg') {
    $path = Join-Path $app $target
    Copy-Item $art $path
    # Copy-Item keeps the candidate's old timestamp, and PreserveNewest and Resizetizer skip files that look older than their last output.
    (Get-Item $path).LastWriteTime = Get-Date
}

@"
<svg xmlns="http://www.w3.org/2000/svg" width="456" height="456" viewBox="0 0 456 456">
  <rect width="456" height="456" fill="$PlateColor"/>
</svg>
"@ | Set-Content (Join-Path $app 'Assets\Icons\icon.svg') -NoNewline

dotnet run $exporter -- $art (Join-Path $app 'Platforms\Windows')
if ($LASTEXITCODE -ne 0) {
    throw 'Exporting the Windows images failed.'
}

Write-Host "App icon set to '$Name'. Rebuild to see it."
