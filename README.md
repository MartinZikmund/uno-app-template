# App Template

## Views

### `IViewBase`

`ViewBase<TViewModel>` is the base class for views (pages). It resolves the view model from the
hosting `WindowShell` service provider and forwards lifecycle events to it.

`IViewBase` is a non-generic interface that exposes the resolved view model as `object?`:

```csharp
public interface IViewBase
{
    object? ViewModel { get; }
}
```

Every `ViewBase<TViewModel>` implements `IViewBase`, so a view can be referenced through the
interface without depending on its concrete view model type.

Use it when:

- A `DataTemplate` (or other loosely-typed code) needs to reach a view's view model without
  knowing its generic argument.
- A test needs to inspect a view's resolved view model without knowing its concrete type at
  compile time.

### Restore dotnet tools

The repository ships a local tool manifest (`.config/dotnet-tools.json`). Restore the tools once after cloning:

```bash
dotnet tool restore
```

### Build and Run

```bash
cd src/AppTemplate
# Windows (requires Windows + WinAppSDK / Windows SDK)
dotnet build -f net10.0-windows10.0.26100
dotnet run -f net10.0-windows10.0.26100

# Cross-platform desktop (macOS, Linux, Windows)
dotnet build -f net10.0-desktop
dotnet run -f net10.0-desktop
```

Swap the target framework (`net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`) to build for other platforms.

## XAML Styler

XAML formatting is kept consistent with [XAML Styler](https://github.com/Xavalon/XamlStyler), pinned as a local dotnet tool. The [`Settings.XamlStyler`](Settings.XamlStyler) file at the repository root holds the rules (aligned with the Uno Platform and Windows Community Toolkit conventions).

From the repository root, format all XAML files under `src/`:

```bash
dotnet xstyler -c Settings.XamlStyler -r -d ./src
```

Or verify formatting without writing changes (useful in CI):

```bash
dotnet xstyler -c Settings.XamlStyler -r -d ./src --passive
```

### Enforcement

Formatting is enforced on every pull request by the **XAML Style Check** workflow
([`.github/workflows/xaml-style-check.yml`](.github/workflows/xaml-style-check.yml)). If a PR contains
unformatted XAML, the check fails, uploads a `xaml-style-patch` artifact, and comments with how to fix it:

- **Branches in this repo:** comment `/apply-xaml-style` on the PR and a bot
  ([`.github/workflows/xaml-style-apply.yml`](.github/workflows/xaml-style-apply.yml)) formats the XAML and
  pushes `chore: Apply XAML styler` to the PR branch.
- **Forks:** download the `xaml-style-patch` artifact and apply it locally (`git apply xaml-style.patch`),
  or just re-run the formatter command above and commit the result.

## Versioning & releases

Versions are produced automatically by
[Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (`nbgv`) from the
version baseline in [`version.json`](version.json) plus the Git commit height. There is no manual
version bumping in source files: the app manifest
(`src/AppTemplate/Package.appxmanifest`) ships with `Identity Version="0.0.0.0"` and the real
version is injected at build time. A CI check
([`validate-manifest-version.yml`](.github/workflows/validate-manifest-version.yml)) enforces that
the manifest stays at `0.0.0.0`.

### `main` produces `-dev` builds

`main` is configured as a public release branch in `version.json`
(`publicReleaseRefSpec`), so builds from `main` get a clean, monotonically increasing version with
an unstable `-dev` suffix derived from the commit height, for example:

```
0.1.0-dev.42
```

These `-dev` builds are intended for continuous integration and internal/preview distribution. They
are **not** published to the app stores.

### `release/<version>` branches produce stable builds

When it is time to ship, cut a **release branch** named `release/<version>` (for example
`release/1.0` or `release/1.2`). Release branches are also matched by `publicReleaseRefSpec`
(`^refs/heads/release/\d+(?:\.\d+)?$`), and because they are not the unstable `main` branch,
`nbgv` drops the `-dev` suffix and produces a **stable** version:

```
1.0.0
```

Stable builds from `release/**` branches are the ones published to the stores (see below).

### Cutting a release with `nbgv`

The repository already references the `Nerdbank.GitVersioning` MSBuild package, so version
stamping happens automatically during every build. To create a release branch, use the `nbgv`
command-line tool, which handles bumping `version.json` on `main` and creating the matching
`release/<version>` branch in one step.

Install the tool once (globally or as a local tool):

```bash
dotnet tool install --global nbgv
# or, as a repo-local tool:
# dotnet new tool-manifest
# dotnet tool install nbgv
```

Then, from a clean `main`, prepare the release:

```bash
nbgv prepare-release
```

This will:

1. Create a `release/<version>` branch (the branch name pattern is configured by
   `release.branchName` in `version.json`, set to `release/{version}`) that carries the current
   `version.json` version. Builds from this branch are stable (no `-dev` suffix).
2. Bump the `version` field in `version.json` on `main` to the next development version, so `main`
   immediately starts producing `-dev` builds for the *next* release.

Inspect the version that will be produced at any time with:

```bash
nbgv get-version
```

### Store publishing

Store packaging happens **only from `release/**` branches** so that store submissions always carry
stable versions:

| Workflow | Output | Store target |
| --- | --- | --- |
| [`package-android.yml`](.github/workflows/package-android.yml) | signed `.aab` | Google Play |
| [`package-ios.yml`](.github/workflows/package-ios.yml) | signed `.ipa` | App Store / TestFlight |
| [`package-windows.yml`](.github/workflows/package-windows.yml) | `.msixupload` | Microsoft Store |

Each of these workflows triggers on `push` to `release/**` branches (and can also be run manually
via `workflow_dispatch`). Pushing to a release branch therefore builds a stable, signed package and
uploads it to the corresponding store. The publishing steps require store/signing secrets to be
configured in the repository (keystores, certificates, service-account keys); without them the
workflows still build artifacts but skip the upload.

`main`, by contrast, only runs the [`ci.yml`](.github/workflows/ci.yml) smoke build and the
[`static-web-apps-deploy.yml`](.github/workflows/static-web-apps-deploy.yml) WASM deployment, both
of which carry `-dev` versions and never publish to the stores.

### Summary

| Branch | Example version | Stable? | Publishes to stores? |
| --- | --- | --- | --- |
| `main` | `0.1.0-dev.42` | No | No |
| `release/<version>` | `1.0.0` | Yes | Yes |

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
