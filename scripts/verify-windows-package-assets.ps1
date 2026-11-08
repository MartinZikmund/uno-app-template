#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies which Windows package images each channel uses and how the manifest is rewritten.

.DESCRIPTION
    Prod points the generated manifest at the padded images in Platforms/Windows; Dev keeps
    Resizetizer's. Either can be overridden with UseWindowsPackageAssets. The rewrite is an
    AfterTargets hook in PackageAssets.targets, which is easy to break by moving a Condition, so it
    is asserted here. See docs/app-icon.md.

    Runs in CI on the template repository only (.github/workflows/template-selftest.yml).

.EXAMPLE
    pwsh scripts/verify-windows-package-assets.ps1
#>
[CmdletBinding()]
param(
    [string]$Project = 'src/AppTemplate/AppTemplate.csproj',
    [string]$TargetFramework = 'net10.0-windows10.0.26100'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$script:failures = 0

function Assert-Case {
    param([string]$Label, [string[]]$MSBuildArgs, [bool]$ExpectWindowsAssets)

    $raw = & dotnet msbuild $Project "-p:TargetFramework=$TargetFramework" @MSBuildArgs `
        '-t:UnoGeneratePackageAppxManifest' '-getProperty:_UseWindowsPackageAssets' '-getItem:AppxManifest' '-getItem:Content' 2>&1
    $text = ($raw | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw "msbuild failed for '$Label':`n$text"
    }
    $result = $text | ConvertFrom-Json

    [xml]$manifest = Get-Content $result.Items.AppxManifest[0].FullPath -Raw
    $ns = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
    $ns.AddNamespace('m', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $ns.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')

    $logo = $manifest.SelectSingleNode('/m:Package/m:Properties/m:Logo', $ns).InnerText
    $visual = $manifest.SelectSingleNode('//uap:VisualElements', $ns)
    $splash = $manifest.SelectSingleNode('//uap:SplashScreen', $ns).GetAttribute('Image')
    $packaged = @($result.Items.Content | Where-Object { $_.Identity -like 'Platforms*Windows*.png' })

    $checks = if ($ExpectWindowsAssets) {
        [ordered]@{
            'flag is true'               = $result.Properties._UseWindowsPackageAssets -eq 'true'
            'logo is PackageLogo.png'    = $logo -eq 'Platforms\Windows\PackageLogo.png'
            'tile is MediumTile.png'     = $visual.GetAttribute('Square150x150Logo') -eq 'Platforms\Windows\MediumTile.png'
            'background is transparent'  = $visual.GetAttribute('BackgroundColor') -eq 'transparent'
            'splash is SplashScreen.png' = $splash -eq 'Platforms\Windows\SplashScreen.png'
            'images are packaged'        = $packaged.Count -ge 8
        }
    }
    else {
        [ordered]@{
            'flag is off'            = $result.Properties._UseWindowsPackageAssets -ne 'true'
            'logo is untouched'      = $logo -notlike 'Platforms\Windows\*'
            'tile is untouched'      = $visual.GetAttribute('Square150x150Logo') -notlike 'Platforms\Windows\*'
            'no images are packaged' = $packaged.Count -eq 0
        }
    }

    foreach ($name in $checks.Keys) {
        if ($checks[$name]) {
            Write-Host "PASS [$Label] $name"
        }
        else {
            Write-Host "FAIL [$Label] $name"
            $script:failures++
        }
    }
}

try {
    Assert-Case 'Prod default' @('-p:AppChannel=Prod') $true
    Assert-Case 'Dev default' @('-p:AppChannel=Dev') $false
    Assert-Case 'Dev, UseWindowsPackageAssets=true' @('-p:AppChannel=Dev', '-p:UseWindowsPackageAssets=true') $true
    Assert-Case 'Prod, UseWindowsPackageAssets=false' @('-p:AppChannel=Prod', '-p:UseWindowsPackageAssets=false') $false
}
finally {
    Pop-Location
}

if ($script:failures -gt 0) {
    Write-Host "$script:failures check(s) failed."
    exit 1
}
Write-Host 'All Windows package asset checks passed.'
