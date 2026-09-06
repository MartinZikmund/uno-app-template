# Windows packaging

How the WinAppSDK head becomes an MSIX, and how that MSIX reaches the Microsoft Store.

## The tool: `winapp` CLI

Packaging uses the [**Windows App CLI**](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/)
(`winapp`), Microsoft's current first-party tool for Windows app packaging, signing and
Store submission. It replaces the older arrangement of
`msbuild /p:GenerateAppxPackageOnBuild=true` in a three-architecture matrix plus a
third-party bundler action.

This is not merely tidier — it is the only path that works here. WinAppSDK 1.7, which
Uno.Sdk 6.7 pins, ships **no bundling targets at all**: `_CreateBundle` and friends live
only in the standalone `Microsoft.Windows.SDK.BuildTools.MSIX` package, which this project
does not restore, and `UapAppxPackageBuildMode` appears zero times in the targets it does
import. `/p:AppxBundle=Always` cannot produce a bundle on this SDK.

Pin the CLI version (`microsoft/setup-WinAppCli@v0.2` with `version: 'v0.6.1'`): winapp is
a 0.x preview and bundling only appeared in 0.3.2.

```powershell
winapp package ./publish/x64 ./publish/arm64 `
  --manifest <generated Package.appxmanifest> `
  --exe AppTemplate.exe `
  --output AppTemplate_<version>.msixbundle
```

One command consumes the per-architecture publish layouts and emits a multi-architecture
`.msixbundle`. `winapp` detects each folder's architecture from its executable's PE header,
generates the PRI and block map, and — for Dev builds — can create and apply a self-signed
certificate in the same call.

Other `winapp` commands the pipeline uses:

| Command | Used for |
|---|---|
| `winapp package` | build the multi-architecture `.msixbundle` |
| `winapp cert generate` | Dev signing certificate, publisher auto-inferred from the manifest |
| `winapp sign` | apply that certificate to the Dev bundle |
| `winapp run`, `winapp ui screenshot`, `winapp unregister` | the packaged smoke launch on `main` builds |

Store submission does **not** go through `winapp store`. It uses
`microsoft/microsoft-store-apppublisher@v1.4` plus the `msstore` CLI directly, so the action
version is pinned and Dependabot can see it. `winapp store` is a passthrough that downloads
the same CLI, and `winapp tool <sdk-tool>` is available if you ever need `makeappx` /
`signtool` / `makepri` with auto-provisioned Build Tools.

### What `winapp package` does not do

It emits no `.appxsym` symbol archive and no `.msixupload` — and neither is needed. The
Store submits the **bare unsigned `.msixbundle`**:

- Partner Center's accepted-format list includes `.msixbundle`. The "always upload a
  `.msixupload`" advice on that page is scoped to *UWP* apps.
- Microsoft's own WinUI Gallery Store pipeline submits a bare `.msixbundle`.
- The Store Developer CLI accepts it: its `PackageFilesExtensionInclude` is
  `[".msix", ".msixbundle", ".msixupload"]`.

Nothing in this repo could produce a `.msixupload` anyway. MakeAppx's own documentation
states it "does not create an app package upload file", and the MSBuild target that does
(`_CreateAppStoreContainerForUAP`) lives only in the standalone
`Microsoft.Windows.SDK.BuildTools.MSIX` package, which is not in this project's restore
graph — which is also why the old `/p:UapAppxPackageBuildMode=CI` was a flag nothing read.

The one thing given up is Partner Center crash symbolication. If you want it back,
WinAppSDK 1.7 does emit a per-architecture `.msixsym`; rename it to `.appxsym` and zip it
together with the bundle into a `.msixupload`.

## Per-architecture publish

```powershell
dotnet publish src/AppTemplate/AppTemplate.csproj `
  -c Release -f net10.0-windows10.0.26100 `
  -r win-arm64 -p:Platform=ARM64 `
  -p:TargetFrameworks=net10.0-windows10.0.26100 `
  -p:GenerateAppxPackageOnBuild=false
```

Two flags are load-bearing and easy to get wrong:

- **`-p:Platform=<x64|ARM64|x86>` is required alongside `-r`.** Without it the build fails
  with `NETSDK1032: The RuntimeIdentifier platform 'win-arm64' and the PlatformTarget 'x64'
  must be compatible`.
- **`-p:TargetFrameworks=<tfm>`** is required alongside `-r` on a multi-targeted Uno project.

Output paths — note that the publish layout does **not** pick up the platform folder, while
`obj/` does:

```
src/AppTemplate/bin/Release/<tfm>/<rid>/publish/                     the layout
src/AppTemplate/obj/[<Platform>/]Release/<tfm>/<rid>/unoresizetizer/m/Package.appxmanifest
```

The workflow locates the manifest with a `find` rather than assuming, because the
`<Platform>` segment appears only for some architectures.

That second path is the **generated** manifest — the one carrying the real identity and
version. The checked-in `src/AppTemplate/Package.appxmanifest` is a template pinned to
`Version="0.0.0.0"`; never pass it to `winapp package`.

## Signing

| Channel | Signing | Why |
|---|---|---|
| **Prod** (`release/v*`) | **unsigned** | The Microsoft Store re-signs every package with a Microsoft certificate and replaces any existing signature. Signing first is wasted work, and signing with the wrong cert is a rejection. |
| **Dev** (`main`) | self-signed, generated in the job | A fork gets a sideloadable package with **zero secrets**. The `.cer` is published alongside so a tester can trust it once. |

`winapp cert generate` infers the certificate subject from the manifest, so it always
matches `Identity/@Publisher` — the mismatch that otherwise fails signing with
`0x8007000B / publisher name does not match`.

Both `BASE64_ENCODED_WINDOWS_PFX` and `BASE64_ENCODED_WINDOWS_PFX_DEV` are retired.

## Store identity — the part that silently fails

Partner Center requires **three** manifest values to match *Product identity* exactly,
case- and punctuation-sensitively:

| Manifest | Repo variable |
|---|---|
| `Package/Identity/Name` | `WINDOWS_STORE_IDENTITY_NAME` |
| `Package/Identity/Publisher` | `WINDOWS_STORE_PUBLISHER` |
| `Package/Properties/PublisherDisplayName` | `WINDOWS_STORE_PUBLISHER_DISPLAY_NAME` |

These are **not** set through `$(ApplicationId)` / `$(ApplicationPublisher)`, and that is
deliberate:

- `ApplicationId` is shared with the Android `applicationId` and the iOS bundle id.
  Rewriting it to a Store identity name would orphan the Play listing.
- Uno.Resizetizer writes the publisher as `O=$(ApplicationPublisher)` — verified against a
  real build, which produced `Publisher="O=Martin Zikmund"`. There is **no** value of that
  property yielding a bare `CN=…`, so a Store publisher routed through it would arrive as
  `O=CN=<GUID>` and be rejected.
- `PublisherDisplayName` comes from the same property, so overriding it would corrupt the
  display name too.

Instead, `src/Directory.Build.targets` post-processes the *generated* manifest with
`XmlPoke` in the `StampWindowsStoreIdentity` target, after
`UnoGeneratePackageAppxManifest`. Leave the variables unset and the manifest is untouched,
so a fork builds green with its own identity.

Getting this wrong produces a **green build** and a Partner Center rejection —
*"The name found in the package is not one of your reserved app names"* — on the one
artifact that is expensive to retry. `_build-windows.yml` therefore asserts all three
values in the generated manifest before packaging.

## Versions

`Identity/@Version` is `X.Y.Z.0`. The Store reserves the fourth section and requires it to
be `0`. See [versioning.md](./versioning.md) for how `X.Y.Z` is derived and why it is
monotonic across patches.

## Store submission

`publish-msstore-draft` (automatic) uploads with `--noCommit`, which stages a draft
**without starting certification** — the Microsoft Store equivalent of Play's internal
track. `commit-msstore` (gated on a required reviewer) then commits it at a 10 % rollout.

One-time setup, which the API cannot do for you:

1. Reserve the app name in Partner Center and complete **one** submission by hand,
   end to end, including the age-ratings questionnaire.
2. Register a **Microsoft Entra ID** application, create a client secret, then Partner
   Center → *Account settings → User management → Microsoft Entra applications* → add it
   with the **Manager** role. Missing this step makes every call return 401. A personal
   Microsoft account will not work.
3. Copy the three *Product identity* values into the repo variables above, and the
   12-character Store id into `MS_STORE_PRODUCT_ID`.

The Store Developer CLI currently supports **free products only**. If the app becomes paid,
swap the Store jobs for raw REST against `manage.devcenter.microsoft.com/v1.0`, or
StoreBroker — both drive the identical API.

## Related

- [release-pipeline.md](./release-pipeline.md) — the workflow map and environments.
- [release-runbook.md](./release-runbook.md) — rollouts, halting, stuck submissions.
- [versioning.md](./versioning.md) — where the version number comes from.
