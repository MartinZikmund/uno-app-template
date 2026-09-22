# Worktree-Scoped App Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give each git worktree its own app identity so two worktrees can be installed and run side by side on Windows, Desktop, Android and iOS, with the worktree name shown next to the version in About.

**Architecture:** Evaluation-time MSBuild detection reads `.git` (a *file* in a linked worktree) with `System.IO` property functions — no `git` subprocess, because `ApplicationId` is decided during evaluation where `Exec` cannot run. A single `_WorktreeIdentityAllowed` gate guards every consumer. Derived tags suffix `ApplicationId` and `ApplicationTitle`; a generated `const` carries the name into C#; a build-time resource overlay carries it into the *localised* Android/iOS display names.

**Tech Stack:** MSBuild (Uno.Sdk), C# 14 / .NET 10, WinUI/Uno XAML, CommunityToolkit.Mvvm, MSTest on Microsoft.Testing.Platform, FluentAssertions.

**Spec:** [`docs/superpowers/specs/2026-09-06-worktree-identity-design.md`](../specs/2026-09-06-worktree-identity-design.md)

## Global Constraints

- **Invariant I1:** `AppChannel=Prod` never carries a worktree suffix, whatever is passed on the command line or exported as an environment variable.
- **Invariant I2/I3:** The main checkout and CI produce identity byte-identical to today.
- **Invariant I4:** `src/AppTemplate/Package.appxmanifest` is never written to (`validate-manifest-version.yml` greps it for `Version="0.0.0.0"`).
- **Invariant I5:** `ApplicationId` ≤ 50 chars, letter-first, only `[a-z0-9.]`. The id segment is always exactly 16 chars.
- **Invariant I6:** `ApplicationTitle` ≤ 40 chars (MSIX `ST_ShortDisplayName`); the long tag is capped at **20**.
- **iOS `CFBundleName` < 16 chars** — the suffixed form abbreviates the base to `AppTmpl`.
- Public override property is **`AppWorktreeName`** (namespaced). Never `WorktreeName` — MSBuild surfaces environment variables as properties.
- Kill switch: `-p:EnableWorktreeIdentity=false`.
- Versions live **only** in `src/Directory.Packages.props`; this feature adds no package.
- New docs go in `docs/<topic>.md` + one line in `docs/README.md`. **Never** append prose to `README.md`.
- Localization keys go in **both** `Strings/en/Resources.resw` and `Strings/cs/Resources.resw`.
- Run `dotnet xstyler -c Settings.XamlStyler -r -d ./src` before committing XAML or CI fails.
- Commits are Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, `build:`).

---

### Task 1: MSBuild detection and derivation

**Files:**
- Modify: `src/Directory.Build.props` (append after the existing `AppChannel` PropertyGroup, currently lines 30–33)
- Test: `scripts/verify-worktree-identity.ps1` (create)

**Interfaces:**
- Produces: `_WorktreeIdentityAllowed` (`true`/empty), `_WtLongTag`, `_WtShortTag`, `_WtIdSegment`, `_WtDevPort`, and the public override `AppWorktreeName`.

All expressions below were verified by evaluation against the fixture table; do not "simplify" the two-step `PadRight`/`Substring` — collapsing them appends the literal text `.PadRight(8,'0')`, which injects a `.` (an MSIX/Android label separator) and fails as a *wrong id* rather than an error.

- [ ] **Step 1: Add the detection + derivation block to `src/Directory.Build.props`**

```xml
    <!-- Worktree identity: appends a per-worktree segment to ApplicationId so two git
         worktrees can be installed and run side by side. No-op in the main checkout and
         in CI (both have a real .git DIRECTORY). See docs/worktree-identity.md. -->
    <PropertyGroup>
        <_RepoRoot>$([MSBuild]::GetDirectoryNameOfFileAbove('$(MSBuildThisFileDirectory)', 'version.json'))</_RepoRoot>
        <_DotGitPath Condition="'$(_RepoRoot)' != ''">$([System.IO.Path]::Combine('$(_RepoRoot)', '.git'))</_DotGitPath>
        <_DotGitText Condition="'$(_DotGitPath)' != '' and $([System.IO.File]::Exists('$(_DotGitPath)'))">$([System.IO.File]::ReadAllText('$(_DotGitPath)').Trim())</_DotGitText>
        <_GitDirRaw Condition="'$(_DotGitText)' != '' and $(_DotGitText.StartsWith('gitdir:'))">$(_DotGitText.Substring(7).Trim())</_GitDirRaw>
        <_GitDirAbs Condition="'$(_GitDirRaw)' != ''">$([System.IO.Path]::GetFullPath($([System.IO.Path]::Combine('$(_RepoRoot)', '$(_GitDirRaw)'))))</_GitDirAbs>
        <_GitDirParent Condition="'$(_GitDirAbs)' != ''">$([System.IO.Path]::GetFileName($([System.IO.Path]::GetDirectoryName('$(_GitDirAbs)'))))</_GitDirParent>

        <!-- Only a LINKED WORKTREE has .git as a file whose gitdir sits under <common>/worktrees/.
             A submodule's .git file points under .../modules/ and is excluded here. -->
        <_WorktreeNameDetected Condition="'$(_GitDirParent)' == 'worktrees'">$([System.IO.Path]::GetFileName('$(_GitDirAbs)'))</_WorktreeNameDetected>
        <AppWorktreeName Condition="'$(AppWorktreeName)' == ''">$(_WorktreeNameDetected)</AppWorktreeName>

        <!-- THE gate. Every consumer checks this and nothing else. -->
        <_WorktreeIdentityAllowed Condition="'$(AppWorktreeName)' != ''
                                         and '$(AppChannel)' == 'Dev'
                                         and '$(CI)' != 'true'
                                         and '$(ContinuousIntegrationBuild)' != 'true'
                                         and '$(EnableWorktreeIdentity)' != 'false'">true</_WorktreeIdentityAllowed>
    </PropertyGroup>

    <PropertyGroup Condition="'$(_WorktreeIdentityAllowed)' == 'true'">
        <!-- Whitelist sanitise: closes MSBuild item-list (;), C# literal (" \) and XML (& < >) escaping at once. -->
        <_WtSan>$([System.Text.RegularExpressions.Regex]::Replace($(AppWorktreeName), '[^A-Za-z0-9 ._-]', ''))</_WtSan>

        <!-- Display name drops a leading worktree-/wt- prefix; the ID keeps the full name, so
             two worktrees that differ only in that prefix still get distinct identities. -->
        <_WtDisp>$([System.Text.RegularExpressions.Regex]::Replace($(_WtSan), '^(worktree|wt)[-_. ]+', ''))</_WtDisp>
        <_WtDisp Condition="'$(_WtDisp)' == ''">$(_WtSan)</_WtDisp>

        <_WtLongTag Condition="$(_WtDisp.Length) &gt; 20">$(_WtDisp.Substring(0, 20))</_WtLongTag>
        <_WtLongTag Condition="'$(_WtLongTag)' == ''">$(_WtDisp)</_WtLongTag>

        <_WtAlnum>$([System.Text.RegularExpressions.Regex]::Replace($(_WtSan.ToLowerInvariant()), '[^a-z0-9]', ''))</_WtAlnum>
        <_WtAlnumPad>$(_WtAlnum.PadRight(8, '0'))</_WtAlnumPad>
        <_WtAlnum8>$(_WtAlnumPad.Substring(0, 8))</_WtAlnum8>
        <!-- 'Sha256' overload is REQUIRED: the 1-arg form changes across MSBuild change waves,
             which would mint a new package identity and orphan the installed app's data. -->
        <_WtHashSha>$([MSBuild]::StableStringHash($(_WtSan), 'Sha256'))</_WtHashSha>
        <_WtIdSegment>wt$(_WtAlnum8)$(_WtHashSha.Substring(0, 6))</_WtIdSegment>

        <_WtInit>$([System.Text.RegularExpressions.Regex]::Replace($(_WtDisp), '([A-Za-z0-9])[A-Za-z0-9]*[^A-Za-z0-9]*', '$1'))</_WtInit>
        <_WtShortSrc Condition="$(_WtInit.Length) &gt;= 2">$(_WtInit.ToUpperInvariant())</_WtShortSrc>
        <_WtShortSrc Condition="'$(_WtShortSrc)' == ''">$(_WtDisp)</_WtShortSrc>
        <_WtShortCut Condition="$(_WtShortSrc.Length) &gt; 4">$(_WtShortSrc.Substring(0, 4))</_WtShortCut>
        <_WtShortCut Condition="'$(_WtShortCut)' == ''">$(_WtShortSrc)</_WtShortCut>
        <_WtShortTag>$(_WtShortCut.Substring(0,1).ToUpperInvariant())$(_WtShortCut.Substring(1))</_WtShortTag>

        <!-- WASM dev port: 5001-5999. 5000 stays reserved for the main checkout. -->
        <_WtDevPort>$([MSBuild]::Add(5001, $([MSBuild]::Modulo($([System.Convert]::ToInt32($(_WtHashSha.Substring(0, 4)), 16)), 999))))</_WtDevPort>
    </PropertyGroup>
```

- [ ] **Step 2: Create the verification script `scripts/verify-worktree-identity.ps1`**

```powershell
#!/usr/bin/env pwsh
# Verifies the worktree-identity derivation and its no-op/leak invariants.
# Usage: pwsh scripts/verify-worktree-identity.ps1
$ErrorActionPreference = 'Stop'
$proj = 'src/AppTemplate/AppTemplate.csproj'
$tfm  = 'net10.0-desktop'
$fail = 0

function Get-AppId([string[]]$extra) {
    $args = @($proj, '-f', $tfm, '-nologo', '-v:diag', '-t:Help') + $extra
    # Cheapest way to read an evaluated property without a full build:
    $out = & dotnet msbuild $proj "-getProperty:ApplicationId" "-p:TargetFramework=$tfm" @extra 2>&1
    return ($out | Out-String).Trim()
}

function Assert-Id([string]$label, [string[]]$extra, [scriptblock]$check) {
    $id = Get-AppId $extra
    if (& $check $id) { Write-Host "  PASS  $label -> $id" -ForegroundColor Green }
    else { Write-Host "  FAIL  $label -> $id" -ForegroundColor Red; $script:fail++ }
}

Write-Host 'Worktree identity invariants:'
Assert-Id 'Dev in worktree gets a suffix'  @()                                { param($i) $i -match '\.wt[a-z0-9]{14}$' }
Assert-Id 'I1 Prod is never suffixed'      @('-p:AppChannel=Prod')            { param($i) $i -eq 'dev.mzikmund.apptemplate' }
Assert-Id 'I1 Prod + forced name'          @('-p:AppChannel=Prod','-p:AppWorktreeName=oops') { param($i) $i -eq 'dev.mzikmund.apptemplate' }
Assert-Id 'I3 CI is never suffixed'        @('-p:CI=true')                    { param($i) $i -eq 'dev.mzikmund.apptemplate.dev' }
Assert-Id 'I3 ContinuousIntegrationBuild'  @('-p:ContinuousIntegrationBuild=true') { param($i) $i -eq 'dev.mzikmund.apptemplate.dev' }
Assert-Id 'Kill switch'                    @('-p:EnableWorktreeIdentity=false') { param($i) $i -eq 'dev.mzikmund.apptemplate.dev' }
Assert-Id 'I5 id length <= 50'             @()                                { param($i) $i.Length -le 50 }

Write-Host ''
if ($fail -gt 0) { Write-Error "$fail invariant(s) FAILED"; exit 1 }
Write-Host 'All worktree identity invariants hold.' -ForegroundColor Green
```

- [ ] **Step 3: Run it and watch it fail on the suffix assertions**

Run: `pwsh scripts/verify-worktree-identity.ps1`
Expected at this point: the first assertion FAILS (no suffix yet — Task 2 applies it); the I1/I3/kill-switch assertions PASS trivially.

- [ ] **Step 4: Confirm the env-var leak vector is closed**

Run:
```powershell
$env:WorktreeName='envleak'; dotnet msbuild src/AppTemplate/AppTemplate.csproj -getProperty:ApplicationId -p:TargetFramework=net10.0-desktop -p:AppChannel=Prod; Remove-Item env:WorktreeName
```
Expected: `dev.mzikmund.apptemplate` — the old un-namespaced name has no effect.

- [ ] **Step 5: Commit**

```bash
git add src/Directory.Build.props scripts/verify-worktree-identity.ps1
git commit -m "build: detect linked git worktrees and derive identity tags"
```

---

### Task 2: Apply the suffix to ApplicationId and ApplicationTitle

**Files:**
- Modify: `src/AppTemplate/AppTemplate.csproj` (insert a third PropertyGroup immediately after the `AppChannel == 'Dev'` group, currently ending line 28)

**Interfaces:**
- Consumes: `_WorktreeIdentityAllowed`, `_WtIdSegment`, `_WtLongTag` from Task 1.
- Produces: suffixed `ApplicationId` / `ApplicationTitle` for all heads.

- [ ] **Step 1: Add the apply group**

```xml
    <!-- Worktree suffix, layered on top of the channel identity. Empty in the main checkout,
         in CI, and for AppChannel=Prod. See docs/worktree-identity.md. -->
    <PropertyGroup Condition="'$(_WorktreeIdentityAllowed)' == 'true'">
        <ApplicationId>$(ApplicationId).$(_WtIdSegment)</ApplicationId>
        <ApplicationTitle>$(ApplicationTitle) ($(_WtLongTag))</ApplicationTitle>
    </PropertyGroup>
```

`ApplicationPublisher` is deliberately **not** touched — it must keep matching the signing certificate subject used by `package-windows.yml`. Icons are **not** touched: `UnoIconBackgroundFile` must stay constant or Android's `@mipmap/icon` breaks (`APT2260`).

- [ ] **Step 2: Run the verification script — all assertions must now pass**

Run: `pwsh scripts/verify-worktree-identity.ps1`
Expected: `All worktree identity invariants hold.`

- [ ] **Step 3: Check the title budget**

Run: `dotnet msbuild src/AppTemplate/AppTemplate.csproj -getProperty:ApplicationTitle -p:TargetFramework=net10.0-desktop`
Expected: `App Template Dev (identity)` — and assert its length is ≤ 40 (invariant I6).

- [ ] **Step 4: Commit**

```bash
git add src/AppTemplate/AppTemplate.csproj
git commit -m "build: suffix ApplicationId and ApplicationTitle per worktree"
```

---

### Task 3: Carry the worktree name into C#

**Files:**
- Modify: `src/AppTemplate/Infrastructure/AppEnvironment.cs`
- Modify: `src/Directory.Build.targets`

**Interfaces:**
- Produces: `AppTemplate.Infrastructure.AppEnvironment.WorktreeName` — `public const string`, `""` when not in a worktree.

- [ ] **Step 1: Make `AppEnvironment` partial**

```csharp
namespace AppTemplate.Infrastructure;

public static partial class AppEnvironment
{
#if APP_CHANNEL_DEV
    public const bool IsDevChannel = true;
    public const string ChannelLabel = "DEV";
#else
    public const bool IsDevChannel = false;
    public const string ChannelLabel = "";
#endif
}
```

- [ ] **Step 2: Add the generation target to `src/Directory.Build.targets`**

The up-to-date check folds `$(AppWorktreeName)` into the **output path**. `Inputs="$(MSBuildAllProjects)"` alone would skip the target after `git worktree move`, leaving a stale constant in `obj/` so the About card lies about which package is installed.

```xml
    <!-- Generates AppEnvironment.WorktreeName. The worktree name is part of the OUTPUT PATH so
         that changing it (git worktree move, or -p:AppWorktreeName=) re-runs the target. -->
    <Target Name="GenerateWorktreeInfo"
            Condition="'$(MSBuildProjectName)' == 'AppTemplate'"
            BeforeTargets="BeforeCompile;CoreCompile">
        <PropertyGroup>
            <_WtStamp Condition="'$(AppWorktreeName)' != ''">$(_WtIdSegment)</_WtStamp>
            <_WtStamp Condition="'$(_WtStamp)' == ''">none</_WtStamp>
            <_WorktreeInfoFile>$(IntermediateOutputPath)AppEnvironment.Worktree.$(_WtStamp).g.cs</_WorktreeInfoFile>
            <_WtNameForCode Condition="'$(_WorktreeIdentityAllowed)' == 'true'">$(_WtDisp)</_WtNameForCode>
        </PropertyGroup>
        <WriteLinesToFile File="$(_WorktreeInfoFile)"
                          Overwrite="true"
                          WriteOnlyWhenDifferent="true"
                          Lines="// &lt;auto-generated /&gt;;namespace AppTemplate.Infrastructure%3B;public static partial class AppEnvironment;{;    public const string WorktreeName = &quot;$(_WtNameForCode)&quot;%3B;}" />
        <ItemGroup>
            <Compile Include="$(_WorktreeInfoFile)" />
            <FileWrites Include="$(_WorktreeInfoFile)" />
        </ItemGroup>
    </Target>
```

- [ ] **Step 3: Build the desktop head and inspect the generated file**

Run: `dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop`
Then: `Get-Content src/AppTemplate/obj/Debug/net10.0-desktop/AppEnvironment.Worktree.*.g.cs`
Expected: `public const string WorktreeName = "identity";`

- [ ] **Step 4: Commit**

```bash
git add src/AppTemplate/Infrastructure/AppEnvironment.cs src/Directory.Build.targets
git commit -m "build: generate AppEnvironment.WorktreeName constant"
```

---

### Task 4: Expose the worktree name through IApplication

**Files:**
- Modify: `src/AppTemplate.Core/Infrastructure/IApplication.cs`
- Modify: `src/AppTemplate/App.xaml.cs` (next to `AppVersion`, lines 19–26)

**Interfaces:**
- Consumes: `AppEnvironment.WorktreeName` (Task 3).
- Produces: `IApplication.WorktreeName` → `string?` (null when not in a worktree).

- [ ] **Step 1: Add the member to `IApplication`**

```csharp
    string AppVersion { get; }

    /// <summary>Git worktree this build came from, or null for the main checkout.</summary>
    string? WorktreeName { get; }
```

- [ ] **Step 2: Implement it on `App`**

```csharp
    public string? WorktreeName =>
        string.IsNullOrEmpty(AppEnvironment.WorktreeName) ? null : AppEnvironment.WorktreeName;
```

Add `using AppTemplate.Infrastructure;` to `App.xaml.cs` if not already present.

- [ ] **Step 3: Build to confirm the interface is satisfied**

Run: `dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/AppTemplate.Core/Infrastructure/IApplication.cs src/AppTemplate/App.xaml.cs
git commit -m "feat: expose the current worktree name through IApplication"
```

---

### Task 5: SettingsViewModel.WorktreeLabel (TDD)

**Files:**
- Create: `tests/AppTemplate.Core.Tests/Fakes/FakeApplication.cs`
- Create: `tests/AppTemplate.Core.Tests/ViewModels/SettingsViewModelTests.cs`
- Modify: `src/AppTemplate.Core/ViewModels/SettingsViewModel.cs` (beside `AppVersion`)

**Interfaces:**
- Consumes: `IApplication.WorktreeName` (Task 4).
- Produces: `SettingsViewModel.WorktreeLabel` → `string?`, formatted via the `WorktreeFormat` resource.

No `IApplication` fake exists yet — create one. House style is hand-written fakes (see `StubServiceProvider` in `tests/AppTemplate.Core.Tests/Infrastructure/IoCTests.cs`), not Moq.

- [ ] **Step 1: Write the fake**

```csharp
using AppTemplate.Core.Infrastructure;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeApplication : IApplication
{
    public ApplicationTheme RequestedTheme { get; set; } = ApplicationTheme.Light;

    public ResourceDictionary Resources { get; } = new();

    public string AppVersion { get; set; } = "1.0.0";

    public string? WorktreeName { get; set; }

    public bool ExitCalled { get; private set; }

    public void Exit() => ExitCalled = true;
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
using FluentAssertions;

namespace AppTemplate.Core.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    [TestMethod]
    public void WorktreeLabel_WhenNotInWorktree_ReturnsNull()
    {
        var vm = CreateViewModel(worktreeName: null);

        vm.WorktreeLabel.Should().BeNull();
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_IncludesTheWorktreeName()
    {
        var vm = CreateViewModel(worktreeName: "identity");

        vm.WorktreeLabel.Should().Contain("identity");
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_UsesTheLocalizedFormatString()
    {
        var localizer = new FakeStringLocalizer { ["WorktreeFormat"] = "WT >> {0}" };
        var vm = CreateViewModel(worktreeName: "identity", localizer: localizer);

        vm.WorktreeLabel.Should().Be("WT >> identity");
    }

    private static SettingsViewModel CreateViewModel(
        string? worktreeName,
        FakeStringLocalizer? localizer = null) =>
        new(
            localizer ?? new FakeStringLocalizer(),
            new FakeAppPreferences(),
            new FakeThemeManager(),
            new FakePreferences(),
            new FakeApplication { WorktreeName = worktreeName });
}
```

Create the remaining collaborator fakes (`FakeStringLocalizer`, `FakeAppPreferences`, `FakeThemeManager`, `FakePreferences`) in `tests/AppTemplate.Core.Tests/Fakes/` in the same hand-written style, each implementing only what the constructor and these tests touch. `FakeStringLocalizer` needs an indexer taking `(string name)` and `(string name, params object[] args)`; the args overload returns `string.Format(stored, args)`.

- [ ] **Step 3: Run the tests and watch them fail**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests"`
Expected: FAIL — `SettingsViewModel` has no `WorktreeLabel`.
(MTP, not VSTest — do **not** pass `--nologo` or `--logger`.)

- [ ] **Step 4: Implement `WorktreeLabel`**

`{markup:Localize}` has no format-argument support, so compose in the view model through the already-injected `_localizer`. Add beside `public string AppVersion => _application.AppVersion;`:

```csharp
    public string? WorktreeLabel =>
        _application.WorktreeName is { Length: > 0 } name ? _localizer["WorktreeFormat", name] : null;
```

- [ ] **Step 5: Run the tests and watch them pass**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests"`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add tests/AppTemplate.Core.Tests src/AppTemplate.Core/ViewModels/SettingsViewModel.cs
git commit -m "feat: add SettingsViewModel.WorktreeLabel"
```

---

### Task 6: Show the worktree in the About card

**Files:**
- Modify: `src/AppTemplate/Views/SettingsView.xaml:52`
- Modify: `src/AppTemplate/Strings/en/Resources.resw` (after the `AboutAppDescription` entry, line 137)
- Modify: `src/AppTemplate/Strings/cs/Resources.resw` (same position)

**Interfaces:**
- Consumes: `SettingsViewModel.WorktreeLabel` (Task 5).

- [ ] **Step 1: Add the resource key to BOTH .resw files**

`en/Resources.resw`:
```xml
  <data name="WorktreeFormat" xml:space="preserve">
    <value>Worktree: {0}</value>
  </data>
```

`cs/Resources.resw`:
```xml
  <data name="WorktreeFormat" xml:space="preserve">
    <value>Pracovn&#237; strom: {0}</value>
  </data>
```

A key present in only one file renders as `???WorktreeFormat???` at runtime.

- [ ] **Step 2: Replace the About card's single `TextBlock` (line 52)**

`StringVisibilityConverter` is already registered in `src/AppTemplate/Resources/Converters.xaml` — no new converter.

```xml
                    <StackPanel HorizontalAlignment="Right" Spacing="2">
                        <TextBlock HorizontalAlignment="Right" Text="{x:Bind ViewModel.AppVersion}" />
                        <TextBlock
                            HorizontalAlignment="Right"
                            Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                            Style="{StaticResource CaptionTextBlockStyle}"
                            Text="{x:Bind ViewModel.WorktreeLabel}"
                            Visibility="{x:Bind ViewModel.WorktreeLabel, Converter={StaticResource StringVisibilityConverter}}" />
                    </StackPanel>
```

- [ ] **Step 3: Format the XAML**

Run: `dotnet xstyler -c Settings.XamlStyler -r -d ./src`
Expected: reformats without error. CI's **XAML Style Check** fails the PR otherwise.

- [ ] **Step 4: Build and visually confirm**

Run: `dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug`
Then launch via the `run-winui-app` skill and screenshot the Settings page. Expected: the version line, with `Worktree: identity` beneath it in secondary text.

- [ ] **Step 5: Commit**

```bash
git add src/AppTemplate/Views/SettingsView.xaml src/AppTemplate/Strings
git commit -m "feat: show the current worktree next to the version in About"
```

---

### Task 7: Suffix the window title

**Files:**
- Modify: `src/AppTemplate/WindowShell.xaml.cs:63-69` (`UpdateWindowTitle`)

**Interfaces:**
- Consumes: `AppEnvironment.WorktreeName` (Task 3).

The window title comes from the *page* title, separate from the Start-menu name. Suffix it so two running windows are distinguishable in the taskbar and Alt-Tab.

- [ ] **Step 1: Modify `UpdateWindowTitle`**

```csharp
    private void UpdateWindowTitle()
    {
        if (ViewModel.Title is not null && !_isWindowClosed)
        {
            _associatedWindow.Title = AppEnvironment.WorktreeName is { Length: > 0 } worktree
                ? $"{ViewModel.Title} — {worktree}"
                : ViewModel.Title;
        }
    }
```

Add `using AppTemplate.Infrastructure;` if not already present.

- [ ] **Step 2: Build and confirm the title**

Run: `dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug`, launch, then `winapp ui list-windows`.
Expected: a window titled `App Template — identity`.

- [ ] **Step 3: Commit**

```bash
git add src/AppTemplate/WindowShell.xaml.cs
git commit -m "feat: include the worktree name in the window title"
```

---

### Task 8: Android localised label overlay

**Files:**
- Modify: `src/Directory.Build.targets` (new target)
- Do **not** modify: `src/AppTemplate/Platforms/Android/Main.Android.cs` — it keeps `Label = "@string/ApplicationName"`

**Interfaces:**
- Consumes: `_WorktreeIdentityAllowed`, `_WtShortTag` (Task 1).

Replacing the resource reference with a generated `const` would destroy the `values-cs` localisation. Instead, rewrite each locale's resource file into `obj/` and swap the item.

- [ ] **Step 1: Add the overlay target**

```xml
    <!-- Appends the worktree short tag to the Android launcher label in EVERY locale, by
         regenerating each values*/Strings.xml into obj/ and swapping the AndroidResource item.
         Keeps localisation intact - Main.Android.cs still points at @string/ApplicationName. -->
    <Target Name="GenerateWorktreeAndroidResources"
            Condition="'$(_WorktreeIdentityAllowed)' == 'true' and '$(TargetPlatformIdentifier)' == 'android'"
            BeforeTargets="PrepareForBuild;_UpdateAndroidResgen">
        <ItemGroup>
            <_WtAndroidStrings Include="@(AndroidResource)" Condition="'%(Filename)%(Extension)' == 'Strings.xml'" />
        </ItemGroup>
        <PropertyGroup>
            <_WtAndroidResDir>$(IntermediateOutputPath)wtres\</_WtAndroidResDir>
        </PropertyGroup>
        <WriteLinesToFile
            File="$(_WtAndroidResDir)%(_WtAndroidStrings.RecursiveDir)Strings.xml"
            Lines="$([System.Text.RegularExpressions.Regex]::Replace($([System.IO.File]::ReadAllText('%(_WtAndroidStrings.FullPath)')), '(&lt;string name=&quot;ApplicationName&quot;&gt;)([^&lt;]*)(&lt;/string&gt;)', '$1$2 [$(_WtShortTag)]$3'))"
            Overwrite="true"
            WriteOnlyWhenDifferent="true"
            Condition="'%(_WtAndroidStrings.Identity)' != ''" />
        <ItemGroup>
            <AndroidResource Remove="@(_WtAndroidStrings)" />
            <AndroidResource Include="$(_WtAndroidResDir)**\Strings.xml" />
        </ItemGroup>
    </Target>
```

- [ ] **Step 2: Build the Android head**

Run: `dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-android -c Debug`
Expected: build succeeds with no `APT2260` and no duplicate-resource error.

- [ ] **Step 3: Verify both locales were rewritten**

Run: `Get-ChildItem -Recurse src/AppTemplate/obj/Debug/net10.0-android/wtres | ForEach-Object { $_.FullName; Get-Content $_.FullName }`
Expected: two `Strings.xml` files (`values`, `values-cs`), each with `App Template [Iden]`.

- [ ] **Step 4: Confirm the applicationId**

Run: `dotnet msbuild src/AppTemplate/AppTemplate.csproj -getProperty:ApplicationId -p:TargetFramework=net10.0-android`
Expected: ends with `.wtworktree1b71ff` (letter-first segment — `GetAndroidPackageName` leaves it alone).

- [ ] **Step 5: Commit**

```bash
git add src/Directory.Build.targets
git commit -m "build: append the worktree tag to the Android label in every locale"
```

**Fallback if aapt rejects the swap (spec open question 5):** if `AndroidResource Remove` does not take effect in the Uno.Sdk pipeline, instead set `$(MonoAndroidResourcePrefix)`-relative copies into a *new* resource directory added via `@(AndroidResource)` with `Link` metadata, and drop the originals with a `Condition` on the item definition in the csproj. Record whichever approach worked in `docs/worktree-identity.md`.

---

### Task 9: iOS localised display-name overlay

**Files:**
- Modify: `src/Directory.Build.targets` (new target)
- Modify: `src/AppTemplate/Platforms/iOS/Info.plist` (lines 42–45)

**Interfaces:**
- Consumes: `_WorktreeIdentityAllowed`, `_WtShortTag` (Task 1).

`CFBundleName` must stay **under 16 characters**, and `App Template` is already 12 — so the suffixed form abbreviates the base to `AppTmpl`, giving `AppTmpl [Iden]` (14).

- [ ] **Step 1: Add the overlay target**

```xml
    <!-- Same idea as the Android overlay: rewrite each .lproj/InfoPlist.strings so the home-screen
         name carries the worktree tag in every locale. CFBundleName must stay under 16 chars,
         so the base abbreviates to AppTmpl when suffixed. -->
    <Target Name="GenerateWorktreeIosResources"
            Condition="'$(_WorktreeIdentityAllowed)' == 'true' and '$(TargetPlatformIdentifier)' == 'ios'"
            BeforeTargets="PrepareForBuild;_CompileAppManifest">
        <ItemGroup>
            <_WtPlistStrings Include="@(BundleResource)" Condition="'%(Filename)%(Extension)' == 'InfoPlist.strings'" />
        </ItemGroup>
        <PropertyGroup>
            <_WtIosResDir>$(IntermediateOutputPath)wtres\</_WtIosResDir>
        </PropertyGroup>
        <WriteLinesToFile
            File="$(_WtIosResDir)%(_WtPlistStrings.RecursiveDir)InfoPlist.strings"
            Lines="$([System.Text.RegularExpressions.Regex]::Replace($([System.IO.File]::ReadAllText('%(_WtPlistStrings.FullPath)')), '&quot;App Template&quot;', '&quot;AppTmpl [$(_WtShortTag)]&quot;'))"
            Overwrite="true"
            WriteOnlyWhenDifferent="true"
            Condition="'%(_WtPlistStrings.Identity)' != ''" />
        <ItemGroup>
            <BundleResource Remove="@(_WtPlistStrings)" />
            <BundleResource Include="$(_WtIosResDir)**\InfoPlist.strings" />
        </ItemGroup>
    </Target>
```

- [ ] **Step 2: Verify the non-localised fallback in `Info.plist` is consistent**

Leave `Info.plist`'s `CFBundleDisplayName`/`CFBundleName` at `App Template`. The `.lproj` strings override them for display in both supported locales (`en`, `cs`, per `CFBundleLocalizations` at line 37), so the fallback is only reachable for an unlisted locale — where the unsuffixed name is the safe answer.

- [ ] **Step 3: Confirm the derived bundle id**

Run: `dotnet msbuild src/AppTemplate/AppTemplate.csproj -getProperty:ApplicationId -p:TargetFramework=net10.0-ios`
Expected: ends with the worktree segment. `Info.plist` declares no `CFBundleIdentifier`, so `CompileAppManifest` fills it from `$(ApplicationId)`.

- [ ] **Step 4: Commit**

```bash
git add src/Directory.Build.targets
git commit -m "build: append the worktree tag to the iOS display name in every locale"
```

**Known limitation to document, not fix:** a per-worktree bundle id matches no existing provisioning profile, so **device** deploys need a wildcard App ID with automatic signing. The simulator is unaffected.

---

### Task 10: WebAssembly dev-server port

**Files:**
- Modify: `src/Directory.Build.targets` (new target)
- Do **not** modify: `src/AppTemplate/Properties/launchSettings.json`, `src/.vscode/launch.json`, `src/.run/AppTemplate.run.xml` — all tracked; per-worktree edits are exactly the recurring-merge-conflict pattern `.claude/rules/docs.md` prevents.

**Interfaces:**
- Consumes: `_WorktreeIdentityAllowed`, `_WtDevPort` (Task 1).

- [ ] **Step 1: Emit the derived port at build time**

```xml
    <!-- WASM has no install identity; isolation is the browser ORIGIN. Two worktrees on :5000
         collide on bind AND share localStorage. Surface a per-worktree port. -->
    <Target Name="ReportWorktreeDevPort"
            Condition="'$(_WorktreeIdentityAllowed)' == 'true' and '$(TargetPlatformIdentifier)' == 'browserwasm'"
            AfterTargets="Build">
        <Message Importance="High" Text="Worktree '$(AppWorktreeName)': run this head on its own origin with --urls http://localhost:$(_WtDevPort)" />
    </Target>
```

- [ ] **Step 2: Spike whether the port can be injected without editing tracked files**

Try, in order, and keep the first that works:
1. `dotnet run -f net10.0-browserwasm --urls http://localhost:<port>`
2. `$env:ASPNETCORE_URLS='http://localhost:<port>'` before `dotnet run`
3. A `$(UnoRemoteControlPort)` / dev-server property, if one exists — check with `dotnet msbuild -getProperty:` and the Uno docs MCP.

Run each from two worktrees simultaneously and confirm both serve and that `localStorage` is not shared (set a preference in one, confirm the other does not see it).

- [ ] **Step 3: Record the winning invocation in `docs/worktree-identity.md`**

If none of the three works without editing a tracked file, the documented answer is the `--urls` flag with the port the build printed in Step 1 — that is an acceptable outcome, not a failure. Say so plainly in the doc.

- [ ] **Step 4: Commit**

```bash
git add src/Directory.Build.targets
git commit -m "build: derive a per-worktree WebAssembly dev-server port"
```

---

### Task 11: CI gates

**Files:**
- Modify: `.github/workflows/ci.yml` (~line 57)
- Modify: `.github/workflows/static-web-apps-deploy.yml`

Once the gates moved onto the apply group (Task 1), `ContinuousIntegrationBuild` stops being belt-and-braces and becomes the mechanism for these two workflows. They currently pass neither `AppChannel` nor `ContinuousIntegrationBuild` and rely on `actions/checkout` producing a real `.git` directory.

- [ ] **Step 1: Add the property to both build steps**

Add `-p:ContinuousIntegrationBuild=true` to the `dotnet build` invocation in each workflow, so all five workflows suppress worktree identity by the same explicit mechanism rather than three doing it incidentally.

- [ ] **Step 2: Confirm no other workflow needs it**

Run: `grep -rn "ContinuousIntegrationBuild\|AppChannel" .github/workflows/`
Expected: `package-windows.yml`, `package-android.yml`, `package-ios.yml` already set one or both; `ci.yml` and `static-web-apps-deploy.yml` now do too.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml .github/workflows/static-web-apps-deploy.yml
git commit -m "ci: mark CI builds explicitly so worktree identity stays suppressed"
```

---

### Task 12: Documentation

**Files:**
- Create: `docs/worktree-identity.md`
- Modify: `docs/README.md` (one line, under `## Building & tooling`, alphabetically between `spec-kit.md` and `xaml-styler.md`)
- Modify: `docs/versioning.md` (lines 21 and 90 — factual corrections; plus a short linking section)
- Modify: `docs/versioning-migration.md` (one step)
- Modify: `.claude/skills/run-winui-app/SKILL.md`
- Modify: `AGENTS.md` (~lines 40–60)
- Modify: `docs/building.md` (~lines 45–60)
- Modify: `README.md` (link only, on the existing **Side-by-side Dev builds** row)

- [ ] **Step 1: Write `docs/worktree-identity.md`**

Lead with what the reader is trying to do ("run two worktrees at once"), then: how detection works, the derived tags with a worked example, the opt-out (`-p:EnableWorktreeIdentity=false`) and override (`-p:AppWorktreeName=`), the per-platform table, and the limitations from spec §9 — `git worktree move` orphans the install, `Get-AppxPackage dev.mzikmund.apptemplate.dev.wt*` finds strays, iOS device deploys need a wildcard App ID, `AppChannel=Prod` from a worktree overwrites the real Prod app, icons are identical across worktrees, `Get-Process AppTemplate` matches every worktree.

Also record the two forward-looking rules, cheapest to write while the surfaces are empty: a future SQLite database must be rooted at `ApplicationData.Current.LocalFolder`, and a future toast COM CLSID or `apptemplate://` scheme is a **machine-global** registration that `ApplicationId` isolation does not cover.

- [ ] **Step 2: Correct the two factual errors in `docs/versioning.md`**

Line 21 and line 90 both claim the Windows Identity Name comes from the signing certificate's Publisher CN. The generated manifest shows `Name` is `$(ApplicationId)` and `Publisher` is `O=$(ApplicationPublisher)`. Fix both, then add a short section linking to `docs/worktree-identity.md`.

- [ ] **Step 3: Update the three copies of the winapp loop**

`.claude/skills/run-winui-app/SKILL.md` is canonical (~19 `-a "App Template"` lines). Change them to capture `ProcessId` from `winapp run --detach --json` and pass `-a <pid>` — `winapp ui status --help` documents `-a` as "process name, window title, or PID. Lists windows if ambiguous" and `-w <HWND>` as taking precedence. Also: lines 59/71 stop quoting a literal `dev.mzikmund.apptemplate.dev` id as fact; note that `$out = Join-Path (Get-Location) …` resolves against the caller's cwd, which registers the wrong build when several worktrees are open; resolve the dangling `docs/winui-run-notes.md` reference at line 218 (the file does not exist); and replace `Get-Process AppTemplate | Stop-Process -Force` with stopping the captured PID, since the former is machine-wide and kills other worktrees.

Then make `AGENTS.md` and `docs/building.md` **link** to the skill rather than restating it — three independent prose copies is how the current drift happened.

- [ ] **Step 4: Add the index line to `docs/README.md` and the link in `README.md`**

One line in `docs/README.md`. In `README.md`, extend the existing **Side-by-side Dev builds** row's sentence with a link. No new prose, no new section — every feature branch that appends to README conflicts with every other one.

- [ ] **Step 5: Commit**

```bash
git add docs .claude/skills/run-winui-app/SKILL.md AGENTS.md README.md
git commit -m "docs: document worktree-scoped app identity"
```

---

### Task 13: End-to-end verification across two worktrees

**Files:** none modified — this task is verification.

- [ ] **Step 1: Build and launch the Windows head from this worktree**

```powershell
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug
$out = Join-Path (Get-Location) "src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"
$a = winapp run $out --exe AppTemplate.exe --detach --json | ConvertFrom-Json
```

- [ ] **Step 2: Do the same from the main checkout at `D:\Personal\uno-app-template`**

Run the identical commands from that directory, capturing `$b`.

- [ ] **Step 3: Assert both are installed and distinct**

```powershell
Get-AppxPackage dev.mzikmund.apptemplate.dev* | Select-Object Name, PackageFamilyName
```
Expected: two entries — `dev.mzikmund.apptemplate.dev` and `dev.mzikmund.apptemplate.dev.wt…` — with different Package Family Names.

- [ ] **Step 4: Assert both windows run simultaneously and are distinguishable**

```powershell
winapp ui screenshot -a $a.ProcessId --output .screenshots\worktree.png
winapp ui screenshot -a $b.ProcessId --output .screenshots\main.png
```
Expected: two live windows; the worktree one titled `App Template — identity`, with `Worktree: identity` under the version in Settings; the main one unsuffixed with no worktree line.

- [ ] **Step 5: Assert app data is isolated**

In one app, Settings → Clear Preferences. Confirm the other app's preferences survive. This is the failure the whole feature exists to prevent.

- [ ] **Step 6: Verify the Desktop head's data folder (spec open question 2)**

```powershell
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop -c Debug
# run it, then:
Get-ChildItem "$env:LOCALAPPDATA\Martin Zikmund" | Select-Object Name
```
Expected: a folder named after the suffixed `ApplicationId`. **If it is not**, the Skia head does not derive its data path from `ApplicationId` and `WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride` must be set in `App.xaml.cs` — add a follow-up task rather than assuming.

- [ ] **Step 7: Clean up**

```powershell
Stop-Process -Id $a.ProcessId -Force
winapp unregister --manifest "$out\AppxManifest.xml"
```

- [ ] **Step 8: Run the full test suite and the invariant script one last time**

```bash
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
pwsh scripts/verify-worktree-identity.ps1
```
Expected: all tests pass; all invariants hold.

---

## As built (2026-09-06)

Implemented across `ac4f3af`, `a9e763c`, `f5c6d90`, `4d10768`. Four things differed from the plan
above; the plan text is left intact so the reasoning is legible, but **prefer this section where
they disagree**.

### 1. Task 8's Android approach does not work — swap the *staged copy*, not the item

Removing and re-adding `@(AndroidResource)` from a target in `Directory.Build.targets` is **silently
ignored**. The Android SDK fixes its resource list before that file can influence it, so the build
*succeeds* and ships the untagged label — the worst failure mode. An earlier variant also hit
`APT2144: invalid file path …res\strings.xml`, because `%(RecursiveDir)` is empty outside a batched
target and the resource landed in `res/` rather than `res/values/`.

What ships instead: `ApplyWorktreeAndroidLabel` hooks `BeforeTargets="_CompileResources"` and patches
the files the SDK has already staged into `obj/…/res/values*/strings.xml`, just before aapt2 compiles
them. The regex excludes `[` from the captured value, so it is idempotent on incremental builds.
Verified: both `values/` and `values-cs/` come out as `App Template [Iden]`.

The iOS target still uses the item-swap shape and is **unverified** — this machine cannot build that
head. It carries `ContinueOnError` and can never run in CI. If the label comes out untagged on a Mac,
give it the same treatment as Android.

### 2. The codegen target must also hook `XamlPreCompile`

`BeforeTargets="BeforeCompile;CoreCompile"` is not enough. The WinAppSDK head runs a full C# compile
pass under `XamlPreCompile` *first*, so the Windows build failed with `CS0117: 'AppEnvironment' does
not contain a definition for 'WorktreeName'` while the desktop head built fine.

### 3. Two extra pieces of work the plan did not anticipate

- **The title bar, not just the window title.** `ExtendsContentIntoTitleBar = true` means the custom
  `win:TitleBar` *is* the visible chrome; `_associatedWindow.Title` only reaches the taskbar and
  Alt-Tab. `WindowShellViewModel.AppTitle` was added and the TitleBar bound to it, so the worktree is
  actually readable on screen.
- **`FakeApplication.Resources` must throw, not construct.** Uno's `net10.0` build is a reference
  assembly: `new ResourceDictionary()` throws `NotSupportedException("Ref assembly")` via
  `NativeDispatcher.GetHasThreadAccess()`. Any future Core test fake touching Uno *object*
  construction hits this — reference *types* are fine, instantiation is not.

### 4. Spec open questions, resolved

| # | Question | Answer |
|---|---|---|
| 1 | MSBuild property → Uno WASM dev server? | **Not attempted.** The build prints the derived port; `--urls` is the documented lever |
| 2 | Does Skia Desktop derive app data from `ApplicationId`? | **Still unverified.** Windows MSIX isolation was proven (two distinct `%LOCALAPPDATA%\Packages` folders); the Skia path was not exercised |
| 3 | Does `StableStringHash(x,'Sha256')` return hex? | **Yes** — 64 hex chars; a 6-char substring is safe |
| 4 | Two concurrent Uno Dev Server instances? | **Not measured** |
| 5 | Does swapping resource items survive aapt? | **No** — see §1 |

### 5. What was verified end to end

Two builds registered and running at once: distinct package family names
(`…apptemplate.dev` vs `…apptemplate.dev.wtworktree1b71ff`), distinct window titles, distinct
`%LOCALAPPDATA%\Packages\…` folders, `Worktree: identity` present in one About card and absent from
the other. Android head builds with both locales tagged. 15 Core tests pass. All seven invariants in
`scripts/verify-worktree-identity.ps1` hold.
