#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies the worktree-scoped app identity derivation and its no-op invariants.

.DESCRIPTION
    Worktree identity must never reach a release artifact and must be a no-op outside a
    linked git worktree. Both guarantees are easy to break by moving a Condition, so they
    are asserted here rather than trusted. See docs/worktree-identity.md.

    Safe to run from the main checkout: there the "in a worktree" assertions are skipped.

.EXAMPLE
    pwsh scripts/verify-worktree-identity.ps1
#>
[CmdletBinding()]
param(
    [string]$Project = 'src/AppTemplate/AppTemplate.csproj',
    [string]$TargetFramework = 'net10.0-desktop'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$script:failures = 0

function Get-Identity {
    param([string[]]$MSBuildArgs = @())

    $raw = & dotnet msbuild $Project "-p:TargetFramework=$TargetFramework" `
        '-getProperty:ApplicationId' '-getProperty:ApplicationTitle' @MSBuildArgs 2>&1
    $json = ($raw | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw "msbuild failed for args '$($MSBuildArgs -join ' ')':`n$json"
    }
    return ($json | ConvertFrom-Json).Properties
}

function Assert-Identity {
    param(
        [string]$Label,
        [string[]]$MSBuildArgs,
        [scriptblock]$Check
    )

    $id = Get-Identity -MSBuildArgs $MSBuildArgs
    if (& $Check $id) {
        Write-Host ('  PASS  {0}' -f $Label) -ForegroundColor Green
        Write-Host ('          {0}  |  {1}' -f $id.ApplicationId, $id.ApplicationTitle) -ForegroundColor DarkGray
    }
    else {
        Write-Host ('  FAIL  {0}' -f $Label) -ForegroundColor Red
        Write-Host ('          {0}  |  {1}' -f $id.ApplicationId, $id.ApplicationTitle) -ForegroundColor Red
        $script:failures++
    }
}

try {
    $inWorktree = (Get-Identity).ApplicationId -match '\.wt[a-z0-9]{14}$'

    Write-Host ''
    Write-Host 'Worktree-scoped app identity invariants' -ForegroundColor Cyan
    Write-Host ('  context: {0}' -f $(if ($inWorktree) { 'linked worktree' } else { 'main checkout' })) -ForegroundColor DarkGray
    Write-Host ''

    # I1 - a release build can never carry a worktree suffix, however it is invoked.
    Assert-Identity 'I1  Prod is never suffixed' `
        @('-p:AppChannel=Prod') `
        { param($i) $i.ApplicationId -eq 'dev.mzikmund.apptemplate' }

    Assert-Identity 'I1  Prod ignores an explicit -p:AppWorktreeName' `
        @('-p:AppChannel=Prod', '-p:AppWorktreeName=oops') `
        { param($i) $i.ApplicationId -eq 'dev.mzikmund.apptemplate' }

    # I3 - CI builds are byte-identical to today.
    Assert-Identity 'I3  CI=true is never suffixed' `
        @('-p:CI=true') `
        { param($i) $i.ApplicationId -eq 'dev.mzikmund.apptemplate.dev' }

    Assert-Identity 'I3  ContinuousIntegrationBuild=true is never suffixed' `
        @('-p:ContinuousIntegrationBuild=true') `
        { param($i) $i.ApplicationId -eq 'dev.mzikmund.apptemplate.dev' }

    # The documented escape hatch.
    Assert-Identity 'Kill switch EnableWorktreeIdentity=false' `
        @('-p:EnableWorktreeIdentity=false') `
        { param($i) $i.ApplicationId -eq 'dev.mzikmund.apptemplate.dev' }

    if ($inWorktree) {
        # I5 - MSIX Identity/Name is capped at 50 chars and must be letter-first lowercase.
        Assert-Identity 'I5  suffixed id is well-formed and <= 50 chars' `
            @() `
            { param($i)
                $i.ApplicationId.Length -le 50 -and
                $i.ApplicationId -cmatch '^[a-z][a-z0-9.]*$' -and
                $i.ApplicationId -match '\.wt[a-z0-9]{14}$' }

        # I6 - MSIX uap:DefaultTile/@ShortName is capped at 40 chars.
        Assert-Identity 'I6  suffixed title is <= 40 chars' `
            @() `
            { param($i) $i.ApplicationTitle.Length -le 40 }
    }
    else {
        Write-Host '  SKIP  suffix assertions (not in a linked worktree)' -ForegroundColor Yellow
    }

    Write-Host ''
    if ($script:failures -gt 0) {
        Write-Host ('{0} invariant(s) FAILED' -f $script:failures) -ForegroundColor Red
        exit 1
    }

    Write-Host 'All worktree identity invariants hold.' -ForegroundColor Green
}
finally {
    Pop-Location
}
