# Release pipeline

You want to know what CI does with your code, and where a package ends up. The short
version:

| Branch | What is built | What is published |
|---|---|---|
| PR to `main` | tests, guards, a Release build of four heads | nothing (plus a WASM preview site) |
| PR to `release/**` | tests, guards, a Release build of four heads | nothing |
| `main` | **all five heads, packaged, Dev channel** | **nothing** |
| `release/vX.Y` | all five heads, packaged, Prod channel | test tracks automatically, public stores after approval |

`main` never publishes. That is the whole point of the split: every push to `main`
produces installable artifacts you can download from the run, and no store ever hears
about them.

## The workflows

| File | Trigger | What it does |
|---|---|---|
| `ci.yml` | PR, push to `main` | Unit tests, the `Package.appxmanifest` pin guard, a version sanity check, and a Release build of desktop / wasm / android / windows. |
| `build-main.yml` | push to `main` | Packages all five heads in the **Dev** channel. Publishes nothing. |
| `release.yml` | push to `release/v**` | Builds all five heads in the **Prod** channel, publishes, tags, and drafts a GitHub Release. |
| `prepare-release.yml` | manual | Cuts `release/vX.Y` from `main` and moves `main` to the next minor. |
| `forward-merge.yml` | push to `release/v**` | Keeps one open `release/vX.Y → main` PR so a hotfix is never lost. |
| `store-ops.yml` | manual | Break-glass: halt or walk a rollout, pause a phased release, unblock a stuck submission, re-push an old run's bits. |
| `store-health.yml` | weekly | Catches asynchronous store failures and un-finalized rollouts. |
| `wasm-pr-preview.yml` | PR to `main` | Per-PR preview site on Azure Static Web Apps. |
| `_build-*.yml` | called | One reusable workflow per head. Never publishes; only ever produces an artifact. |

The five `_build-*.yml` workflows are the only place a head is *packaged*. `build-main.yml`
and `release.yml` both call them, so a Dev package and a Prod package are produced by
identical code paths — the channel is an input, not a different script. (`ci.yml` and
`wasm-pr-preview.yml` also compile heads, but only to check they build; they package
nothing.)

## Build once, publish many

A `release.yml` run builds each head exactly once and every publish job downloads the
artifact from that same run. A publish never recompiles, because a rebuild is a different
binary and — since the version is the git height — a different version number. Retrying a
store push must not change what you are shipping.

Each publish job `needs:` **only** the build job whose artifact it consumes. A red desktop
leg cannot block Play; a Play failure cannot block TestFlight.

## What is automatic and what waits for you

Automatic on every `release/v*` push:

- **Google Play** → `internal` track
- **TestFlight** → uploaded and processed
- **Microsoft Store** → draft submission, uploaded with `--noCommit` so certification has
  *not* started
- **Azure Static Web Apps** → production
- the `vX.Y.Z` git tag and a **draft** GitHub Release carrying the desktop zips, the
  Windows `.msixbundle` and its `.cer`

Waiting for a one-click approval in the run's *Review deployments* prompt:

- `play-production` → production track at 10 %, `inProgress`
- `appstore-production` → submit for review, phased release, auto-release on approval
- `ms-store-production` → commit the submission at 10 % rollout

The rule is: **gate on "a member of the public sees this", not on "a store received a
file".** A store receiving a file is recoverable; a bad build reaching users is not.

To make everything fully automatic, remove the required reviewers from those three
environments — the workflow needs no edit.

## Environments

Environment secrets are unreachable until the environment's protection rules pass, which
is the larger half of the benefit — bigger than the approval prompt.

| Environment | Reviewers | Branch policy | Holds |
|---|---|---|---|
| `play-internal` | none | `release/v*` | Play service account |
| `testflight` | none | `release/v*` | ASC API key |
| `ms-store-draft` | none | `release/v*` | Partner Center credentials |
| `web-production` | none | `release/v*` | SWA token |
| `play-production` | **1 required** | `release/v*` | Play service account |
| `appstore-production` | **1 required** | `release/v*` | ASC API key |
| `ms-store-production` | **1 required** | `release/v*` | Partner Center credentials |
| `store-ops` | **none** | **none** | copies of all of the above |

`store-ops` is deliberately reviewer-free and branch-unrestricted. `gh workflow run`
dispatches on the default branch, so binding the break-glass levers to `play-production`
would make every emergency halt either refused (*"Branch main is not allowed to deploy
to…"*) or stuck waiting for approval — dead exactly in the situation they exist for.
Environment secrets do not inherit, so `store-ops` needs its own copies.

Do **not** enable *prevent self-review* on a solo-maintainer repo; you would deadlock
yourself.

## Forks and missing secrets

Every packaging job is gated on
`github.event.repository.private == false || github.event_name == 'workflow_dispatch'`,
so a private fork does not burn paid Actions minutes on every push.

**A missing secret is never a build failure.** Each publish job checks for its credential
and skips green with a `::warning::` naming what is absent. Concretely, a fresh fork with
no secrets at all gets:

- an **unsigned** Android AAB (and the Play jobs skip)
- an iOS **simulator** `.app.zip` instead of an `.ipa` — a device `.ipa` is by definition
  a signed, provisioned archive, so there is no unsigned equivalent
- a self-signed, sideloadable Windows package plus its `.cer`
- desktop zips and a WASM site
- green checkmarks throughout — the gated jobs still request approval, and skip green with a
  warning once approved rather than failing on the missing credential

## Switching heads off

Repo variables, no YAML edit:

| Variable | Effect |
|---|---|
| `ENABLE_ANDROID`, `ENABLE_IOS`, `ENABLE_WINDOWS`, `ENABLE_DESKTOP`, `ENABLE_WASM` | set any to `false` to skip that head's **packaging** in `build-main.yml` and `release.yml`. `ci.yml`'s PR build matrix is not affected — edit it directly to stop compiling a head altogether. |
| `ENABLE_DESKTOP_MACOS_ON_MAIN` | `true` adds `osx-arm64` to `main` builds (macOS runners bill at 10×; off by default, always on for releases) |

`workflow_dispatch` on `build-main.yml` and `release.yml` also takes an `only` input
(`android,windows`) to build a single head while iterating.

## The GitHub App, and why the fallback exists

A push made with `GITHUB_TOKEN` **does not trigger workflows**. That single rule decides
the shape of `prepare-release.yml`: if the bot pushes `release/v1.0` with `GITHUB_TOKEN`,
`release.yml` never starts.

Two supported paths:

- **Configured** — set `vars.RELEASE_APP_ID` and `secrets.RELEASE_APP_PRIVATE_KEY` for a
  GitHub App with `contents: write`. Its push triggers `release.yml` normally.
- **Not configured** (every fresh fork) — the workflow pushes with `GITHUB_TOKEN` and then
  starts the release with a single `gh workflow run`, which is the documented exception
  that always creates a run. A `::warning::` names the upgrade.

A **patch needs neither.** Pushing a commit to `release/vX.Y` yourself is an ordinary human
push and triggers `release.yml` the normal way.

## Secrets and variables

| Secret | Used by | Missing ⇒ |
|---|---|---|
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD` | Android build | unsigned AAB, job green, Play jobs skip |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | Play publish | Play jobs skip green |
| `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD`, `APPLE_PROVISIONING_PROFILE_BASE64`, `APPLE_CODESIGN_KEY`, `APPLE_TEAM_ID`, `KEYCHAIN_PASSWORD` | iOS build | simulator build; TestFlight and App Store jobs skip |
| `APPLE_ADHOC_PROVISIONING_PROFILE_BASE64` | iOS build on `main` | `main` falls back to a simulator build |
| `APPSTORE_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_PRIVATE_KEY` | TestFlight, review submission | those jobs skip green |
| `MS_STORE_TENANT_ID`, `MS_STORE_SELLER_ID`, `MS_STORE_CLIENT_ID`, `MS_STORE_CLIENT_SECRET` | Microsoft Store | Store jobs skip; the `.msixbundle` artifact stays available for manual upload |
| `AZURESTATICWEBAPPSDEPLOYMENTTOKEN` | web deploy | skips green |
| `RELEASE_APP_PRIVATE_KEY` | release cut, tag | degrades to the `GITHUB_TOKEN` + dispatch path |

The Microsoft Store credential must be a **Microsoft Entra ID** application — a personal
Microsoft account will not authenticate.

| Variable | Purpose |
|---|---|
| `ANDROID_PACKAGE_NAME` | Play listing id (default `dev.mzikmund.apptemplate`) |
| `APPSTORE_APP_ID` | numeric App Store Connect app id, used by every review-submission call |
| `APPLE_PROVISIONING_PROFILE_NAME` | profile **name**, not UUID — names survive regeneration, UUIDs do not |
| `MS_STORE_PRODUCT_ID` | 12-character Store id from Partner Center |
| `WINDOWS_STORE_IDENTITY_NAME`, `WINDOWS_STORE_PUBLISHER`, `WINDOWS_STORE_PUBLISHER_DISPLAY_NAME` | the three Partner Center → *Product identity* values, stamped into the manifest. Unset ⇒ the manifest is left alone. See [windows-packaging.md](./windows-packaging.md). |
| `VERSION_CODE_OFFSET` | only for an app already live on Play above the computed versionCode |
| `RELEASE_APP_ID` | GitHub App id; empty selects the fallback path |

## Related

- [release-runbook.md](./release-runbook.md) — cutting, patching, halting: the operational steps.
- [versioning.md](./versioning.md) — how a version number becomes each store's version.
- [windows-packaging.md](./windows-packaging.md) — the MSIX path and Microsoft Store identity.
