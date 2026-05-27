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

### `main` produces stable, publishable builds

`main` is the only public-release branch in `version.json` (`publicReleaseRefSpec` is
`^refs/heads/main$`), so builds from `main` get a clean, monotonically increasing **stable**
version with no prerelease suffix, for example:

```
0.1.0
```

Every push to `main` is what gets packaged and published to the stores (see below).

### Other branches produce `-beta` builds

Any branch that is not `main` — feature branches, pull requests, release maintenance branches — is
treated as non-public, so `nbgv` appends an unstable `-beta` suffix (configured by
`firstUnstableTag`) plus the commit-height metadata, for example:

```
0.1.0-beta.g1a2b3c4
```

These `-beta` builds are intended for local development and CI validation only. They are **not**
published to the app stores.

### Bumping the version

The version baseline lives in the `version` field of [`version.json`](version.json). Bump it
whenever you want the next stable version to change (for example from `0.1` to `0.2`), commit the
change to `main`, and the next push will package and publish under the new version.

For a more formal flow, install the `nbgv` command-line tool and let it manage the bump and an
optional `release/v{version}` maintenance branch in one step:

```bash
dotnet tool install --global nbgv
# or, as a repo-local tool:
# dotnet new tool-manifest
# dotnet tool install nbgv
```

```bash
nbgv prepare-release
```

This creates a `release/v{version}` branch (the pattern configured by `release.branchName`) for the
current version and bumps `version.json` on `main` to the next development version. Inspect the
version that will be produced at any time with:

```bash
nbgv get-version
```

### Store publishing

Store packaging runs on every push to `main` (and can also be run on demand via
`workflow_dispatch`), so store submissions always carry stable versions:

| Workflow | Output | Store target |
| --- | --- | --- |
| [`package-android.yml`](.github/workflows/package-android.yml) | signed `.aab` | Google Play |
| [`package-ios.yml`](.github/workflows/package-ios.yml) | signed `.ipa` | App Store / TestFlight |
| [`package-windows.yml`](.github/workflows/package-windows.yml) | `.msixupload` | Microsoft Store |

The publishing steps require store/signing secrets to be configured in the repository (keystores,
certificates, service-account keys); without them the workflows still build artifacts but skip the
upload.

Pull requests run only the [`ci.yml`](.github/workflows/ci.yml) smoke build, and the
[`static-web-apps-deploy.yml`](.github/workflows/static-web-apps-deploy.yml) workflow deploys the
WASM head from `main`; neither publishes to the stores.

### Summary

| Branch | Example version | Stable? | Publishes to stores? |
| --- | --- | --- | --- |
| `main` | `0.1.0` | Yes | Yes |
| feature / PR / `release/v*` | `0.1.0-beta.g1a2b3c4` | No | No |

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
