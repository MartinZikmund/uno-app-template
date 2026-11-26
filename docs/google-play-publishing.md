# Publishing to Google Play

Everything it takes to get an app from this template onto Google Play the first time: accounts, keys, the store
listing, the in-app product, RevenueCat, every App content declaration, and the traps found on the way. The CI side
(what `release.yml` builds and uploads) is in [release-pipeline.md](./release-pipeline.md); this page is the console
side and the decisions around it.

It was written while publishing Merry Countdown (a kids' app with one non-consumable in-app purchase) in November
2026. Values from that app are shown as examples. Each step says who has to do it:

- **Agent**: an agent driving the browser can do it.
- **Human**: needs the account owner, because it accepts terms, certifies something legally, types a password, or the
  browser automation's permission guard refuses it. Hand these over as one-click tasks with the form already filled.

## Checklist

For agents: work through this in order. Each line links to its section.

1. [ ] [Check the developer account](#1-check-the-developer-account): testing requirement, trader status.
2. [ ] [Create the app in Play Console](#2-create-the-app) (**Human**).
3. [ ] [Upload key and signing secrets](#3-upload-key-and-signing-secrets).
4. [ ] [Service accounts for CI and RevenueCat](#4-service-accounts).
5. [ ] [First upload](#5-first-upload): the API works for a new app, as a *draft*.
6. [ ] [Store listing text and graphics](#6-store-listing), per language.
7. [ ] [Store settings](#7-store-settings): category, contact email.
8. [ ] [In-app product, price and sale](#8-in-app-product).
9. [ ] [RevenueCat project](#9-revenuecat).
10. [ ] [App content declarations](#10-app-content-declarations) (some **Human**).
11. [ ] [Internal test, then production](#11-internal-test-and-production).

## 1. Check the developer account

**Agent.** Open Play Console → app list → the app's dashboard.

- **Closed testing requirement.** Personal accounts created on or after 2023-11-13 must run a closed test with 12
  testers for 14 days before they get production access. If the dashboard's *Release your app* section goes straight
  to **Create and publish a release** on the production track, with no *Apply for production* step, the account is
  exempt. If not, start the closed test immediately: it is the longest lead time of the whole process.
- **Trader status (EU DSA).** If the *account* is declared a trader, your address and phone are already public on
  Play, and each app only needs to confirm it (section 10). Selling an in-app purchase as a business or freelancer
  makes you a trader.

## 2. Create the app

**Human.** Play Console → *Create app*. The creation form has declarations (developer program policies, US export
laws) that browser automation is not allowed to accept. Use the package name the template sets for the Prod channel
(`ApplicationId` / `ANDROID_PACKAGE_NAME`). The package name can never change.

## 3. Upload key and signing secrets

Play App Signing holds the real app signing key; CI only signs with an **upload key**. The first bundle Play receives
fixes which upload key it expects from then on.

- **Decision: reuse one upload keystore across your apps or make one per app.** Either works. If one already exists,
  use it, so there is only one key to keep safe. Merry Countdown reused the owner's existing keystore.
- **Human** pastes the values into GitHub (Settings → Secrets and variables → Actions). An agent can open each
  secret's page with the name filled in; the owner pastes the value. That keeps passwords out of agent transcripts.

| Secret | Value |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | `[Convert]::ToBase64String([IO.File]::ReadAllBytes("upload.keystore"))` |
| `ANDROID_KEY_ALIAS` | the key alias |
| `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD` | the passwords (the same for a PKCS12 keystore) |

Check that the secrets work by running a Dev build on `main`: it signs with the same key and needs no Play access.

Caveat: an upload key can be reset through Play support if lost, but it takes days. Keep the keystore and its
passwords in a password manager, not only in GitHub.

## 4. Service accounts

Two different Google Cloud service accounts, each with its own JSON key:

| Account | Used by | Play Console permissions |
| --- | --- | --- |
| uploader, e.g. `googleplayuploader@<project>.iam.gserviceaccount.com` | GitHub Actions: `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | releases: the default set when adding the app (9 permissions) |
| `revenuecat@<project>.iam.gserviceaccount.com` | RevenueCat's Play app configuration | view app information, view financial data, manage orders and subscriptions |

- Service accounts are invited in Play Console → *Users and permissions*. Permissions are **per app** unless granted
  account-wide, so every new app has to be added to both accounts: open the account → *Add app* → tick the app →
  *Apply* → *Save changes* → confirm. **Agent.**
- A JSON key is downloadable **once**, at creation. If it is lost, create a new key on the same service account
  (Google Cloud → IAM → Service accounts → the account → *Keys* → *Add key* → *JSON*). That keeps all its Play
  permissions. Don't delete old keys that other projects may still use. Creating a key is an **Agent** step if the
  owner asked for it. The file lands in Downloads, so tell the owner to move it into a password manager.
- Don't mix the two up: uploading the uploader's key to RevenueCat fails validation.

## 5. First upload

Older advice says the first bundle of a new app must go through the console. That is no longer true: the Publishing API
accepts it, but **only as a draft** (`status: draft`). Play refuses any other status for an app that has never been
published (*"Only releases with status draft may be created on draft app"*).

Caveat for this template: `release.yml` uploads to the internal track with `status: completed`, so a brand-new app's
first release fails at *publish-play-internal*. Until the app's first publish, either upload with `status: draft`
or upload the first bundle by hand in Play Console. After that, `completed` is right.

A draft on the internal track then needs testers (an email list) and *Save and publish*. Internal releases are not
reviewed and are live for testers within minutes. **Agent.**

## 6. Store listing

Grow users → Store presence → *Store listings* → default listing. Add more languages with *Manage languages*.

### Text

| Field | Limit | Notes |
| --- | --- | --- |
| App name | 30 | Keyword stuffing is against policy; Merry Countdown kept just the name. |
| Short description | 80 | Count characters in code; diacritics count as one. |
| Full description | 4000 | Drop platform-specific claims (e.g. Windows-only features). |

### Graphics

| Asset | Size | Rules |
| --- | --- | --- |
| App icon | 512 × 512 PNG, ≤ 1 MB | Full-bleed square; Play applies the mask. |
| Feature graphic | 1024 × 500 PNG/JPEG, ≤ 15 MB | No alpha. Keep text away from the edges. |
| Phone screenshots | 2–8, 9:16 or 16:9, each side 320–3840 px, ≤ 8 MB | Promotion needs ≥ 4 at ≥ 1080 px. 2160 × 3840 works. |
| 7" and 10" tablet screenshots | up to 8 each, 16:9 or 9:16 | 10" needs each side ≥ 1080 px. 3840 × 2160 fits both. |

How Merry Countdown's set was made, reusable for any app:

- Captures from **emulators created for the job** (Pixel 9 Pro 1280 × 2856, Pixel Tablet 2560 × 1600, data partition
  8 GB), not the owner's existing AVDs. A 150 MB all-ABI Debug APK doesn't fit a default 6 GB AVD; build with
  `-p:RuntimeIdentifier=android-x64`.
- Capture build: Debug (for the seeding hooks) with `-p:AppChannel=Prod -p:UnoDisableHotDesign=true`. That removes the
  DEV badge and the Hot Design toolbar; comment out `MainWindow.UseStudio()` in the throwaway capture worktree, which
  doesn't compile without Hot Design.
- Seed data with `adb push` to `/data/local/tmp`, then `run-as <package> cp … files/` (works for debuggable builds on
  Play Store images). Debug-only hook files in `LocalFolder` set the language, the date and a simulated purchase.
- Clean status bar: `settings put global sysui_demo_allowed 1`, then `am broadcast -a com.android.systemui.demo` with
  `clock -e hhmm 0900`, `battery -e level 100`, `network -e mobile hide`, `network -e wifi show -e level 4 -e fully true`,
  `notifications -e visible false`.
- Dark theme (`cmd uimode night yes`), if it matches the other stores' screenshots.
- Animated scenes: take a burst of screenshots and pick the frame programmatically (for example by the position of a
  coloured object).
- Frame the captures in HTML pages rendered by headless Chrome at the exact size
  (`chrome --headless=new --screenshot --window-size=W,H`). Draw large and use CSS `zoom` for small outputs like the
  1024 × 500 feature graphic.

### Upload caveats

- Graphics are added through a side panel (asset library). Its hidden `input[type=file]` takes files directly.
- Upload **one file at a time** and wait about 10 seconds between them. Batched or quick uploads finish in random
  order, and some silently never arrive. Verify each slot's contents and order afterwards (each item has a
  `Remove <filename>` button whose label gives the order).
- An asset already in the library is added to the active slot through its row's arrow, then *Add*. That's the easiest
  way to fill the second tablet slot, or to fix the order without dragging.
- A new language starts with faded copies of the default language's graphics. Uploading a graphic for that language
  replaces the inherited set for that slot.
- *Save as draft* often. Nothing is published until *Send for review* in Publishing overview.

## 7. Store settings

Store presence → *Store settings*: app category (Merry Countdown: *Parenting*), and the contact email shown on the
listing. **Agent.**

## 8. In-app product

Monetize → Products → *One-time products*. Play only lets you create one after a bundle that declares billing
(`com.android.vending.BILLING`, added by the billing library, e.g. the RevenueCat SDK) has been uploaded, so do the first upload first.

- **Product ID** is permanent and must match the app's code and the RevenueCat product.
- **Purchase option**: id e.g. `lifetime`, type *Buy*, marked **Backwards compatible**, which RevenueCat needs.
- **Price**: *Set prices* → *Bulk edit pricing* → select all regions → one price and currency. Play converts and
  charms it per country (US$2.99 became 79.99 CZK and €3.29).
- **Sale**: *Add offer* → *Discount* → *Percentage* → *Set discount* for all regions. A 40% discount on US$2.99 is
  exactly US$1.79. Start and end times are in UTC, and the start can't be in the past.
- **Activation**: the purchase option first, then the offer (Play refuses the other order). Activating doesn't
  publish the app.
- **Translations**: *Edit one-time product details* → *Manage translations*.
- **Decision**: keep the product's name the same as the in-app wording. Merry Countdown's app said "Full Version"
  while the stores said "Pro", which confuses reviewers.

## 9. RevenueCat

The template doesn't include a billing library. Merry Countdown sells through RevenueCat; if your app does too:

**Agent**, once the owner has logged in (agents can't type passwords).

1. A **project** per app (*Create new project*). Skip the setup wizard: it proposes monthly and yearly subscriptions.
2. *Apps* → *New app configuration* → *Google Play Store*: app name, package name. Check the saved name: typing into a
   prefilled field produced a doubled name.
3. Upload the **revenuecat** service account JSON (section 4). Validation shows "package name not found" for
   purchases until a release has been rolled out to some track; the catalog checks pass straight away.
4. *Product catalog* → *Products* → *New product*: non-consumable, Google product identifier = the Play product ID.
5. *Entitlements* → new entitlement, then *Attach* the product.
6. *Offerings* → new offering `default`, with a *Lifetime* package containing the product. The first offering becomes
   *current* (check mark). Merry Countdown's store service sells the first package of the current offering.
7. *API keys* → the app's public SDK key (`goog_…`) goes into the app's store configuration. Public keys are designed
   to ship inside apps; secret keys never do.

## 10. App content declarations

Policy → *App content*. All are saved as drafts and go out with *Send for review*. Order matters: **Sign in details**
before **Target audience**, and **Content rating** before Target audience (until a rating exists, age groups under 13
are locked).

| Declaration | Merry Countdown's answer | Who | Notes |
| --- | --- | --- | --- |
| Privacy policy | URL | Agent | Must be public, about this app. |
| Ads | No | Agent | Can change in a later update; see [ads](#ads). |
| Sign in details | Yes, with instructions | Agent | See below. |
| Content rating | IARC questionnaire | **Human** ticks IARC terms, Agent answers | See below. |
| Target audience | 5 and under, 6–8, 9–12 | **Human** certifies COPPA/GDPR, Agent does the rest | See below. |
| Data safety | Purchase history, Device or other IDs: collected, not shared | Agent | Needs Target audience first. |
| Advertising ID | No | Agent | See [the AD_ID trap](#the-ad_id-trap). |
| Government apps | No | Agent | |
| Financial features | None | Agent | |
| Health apps | None | Agent | |
| Trader status | Yes, plus a Terms of Service URL | **Human** saves | The ToS URL is shown publicly; host a page next to the privacy policy. |

### Sign in details

Answer **Yes** if any content needs a purchase, even without accounts. Google's list of restrictions includes
"payments, such as one-time products". The required checkbox states that the instructions give *full access including
paid content*, so reviewers need a free way into the paid part:

- Monetize → *Promo codes* → *Create promo code* (the first time, **Human** or explicit approval: it accepts the Promo
  codes Terms of Service). Type *One-time product* → the purchase option, up to 1 year, N single-use codes. Play has
  no reusable custom codes for one-time products.
- Download the codes (CSV), put 2–3 in the instructions (500 characters max) and keep spares for the next review.
- Describe the path to the purchase in the app's own words, and how to pass the parent gate.
- Turn **off** "allow Google to use these sign-in details for testing on partner devices", or automated testing
  uses up the single-use codes.

### Content rating

Category *All Other App Types*. Answer from what the app really does, and keep it consistent with the other stores'
IARC answers. Merry Countdown: online content **yes** (doors link to YouTube videos), digital purchases **yes**, random
items **no**, everything else **no**. Result: ESRB Everyone, PEGI 3, Brazil 14+ (because of the online videos, the
same as on the Microsoft Store).

### Target audience

- **Decision: kids only, or kids and adults?** Kids only (all groups under 13) puts the app in *Designed for Families*
  with no age screen. Adding 13+ groups makes it a mixed-audience app, which needs a neutral age screen.
- Under-13 groups mean the **Families policy** applies: child-appropriate content, only Families-certified ad SDKs,
  COPPA/GDPR compliance, and no advertising ID.
- *Teacher Approved*: optional, reviewed separately, doesn't hold up the release. Merry Countdown opted in.

### Data safety

Declare what SDKs send off the device, even when you never see it. With RevenueCat, for example: *Financial info → Purchase
history* and *Device or other IDs* (RevenueCat's anonymous app user ID). Both collected, not shared (a service provider
isn't sharing), not ephemeral, required, purpose *App functionality*. Encrypted in transit: yes. No accounts. Deletion:
a URL that explains how to request it (the privacy policy's deletion section works). Locally stored data (for example
photos copied into app storage) isn't "collected" and isn't declared. Keep the answers word-for-word consistent with
the privacy policy.

### The AD_ID trap

`Kebechet.Maui.RevenueCat.Android` depends on `Xamarin.GooglePlayServices.Ads.Identifier`, whose manifest declares
`com.google.android.gms.permission.AD_ID`. It ends up in every RevenueCat app's merged manifest, which contradicts an
"Advertising ID: No" declaration and the Families policy. RevenueCat only reads the ID if the app calls
`collectDeviceIdentifiers`. Remove the permission in `Platforms/Android/AndroidManifest.xml`:

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android" xmlns:tools="http://schemas.android.com/tools">
  <uses-permission android:name="com.google.android.gms.permission.AD_ID" tools:node="remove" />
</manifest>
```

Check with `aapt dump permissions <apk>`. To find who adds a permission, search the unpacked library manifests:
`grep -rl permission.AD_ID obj/**/lp/*/jl/AndroidManifest.xml`, then the package's dependencies in
`obj/project.assets.json`.

### Ads

Adding ads later is an ordinary update: change the *Ads* declaration and data safety at the same time. For a Families
app: only Families self-certified ad SDKs, child-directed requests (e.g. AdMob `tagForChildDirectedTreatment`), no
personalised ads, and the advertising ID stays removed. Every store listing and the privacy policy that promise "no
ads" have to change too. Apple's Kids Category is stricter still.

## 11. Internal test and production

- Roll out internal releases freely: not reviewed, testers only, and it makes RevenueCat's purchase validation pass.
- Before production, build from the release branch so the version name has no commit hash (the template passes
  `ApplicationDisplayVersion` explicitly; an app that leaves it to Nerdbank.GitVersioning gets `X.Y.Z-g<hash>` outside
  release branches).
- The *Send for review* in Publishing overview submits the listing, product and declarations together with the
  release. Review of a new kids' app takes days, so leave a week's margin before any date that matters.

## Automating Play Console

Notes for agents driving Play Console in a browser.

- Many deep links redirect to the account home. Open the app dashboard and read the real URLs from its links:
  `…/app/<id>/store-listings`, `…/store-settings`, `…/one-time-products`, `…/promotions`, `…/app-content/overview`,
  `…/tracks/internal-testing`. Declarations live under `…/app-content/<name>` (for example `testing-credentials`,
  `target-audience-content`, `data-privacy-security`, `trader-status`, `content-rating-iarc-questionnaire`).
- *Start declaration* buttons have the aria-label `Start <name> declaration`.
- The forms are Angular. Set text with the native value setter plus `input` and `blur` events, or the counters and
  validation don't update. Pick radio options by clicking their `label`, scoped to the `[role=radiogroup]` that
  belongs to the question. Never answer questions blind in a loop: read each question's text first.
- Data safety's data-type sections are an accordion: opening one closes the other, but the ticks stay.
- Date pickers take typed dates (`Dec 2, 2026` then Enter). Time pickers list hours in UTC, and past hours are disabled.
- A save that seems to do nothing usually opened a confirmation dialog: take a screenshot.
- These can't be done by an agent, even when asked: typing passwords, ticking agreements (IARC terms, the COPPA
  certification), and some saves the permission guard refuses. Fill the form, then hand over one click.

## Related

- [release-pipeline.md](./release-pipeline.md): the workflows, environments and secrets.
- [release-runbook.md](./release-runbook.md): staged rollout, halting, finalizing.
- [versioning.md](./versioning.md): versionCode and version names.
<!--#if (hasWindows)-->
- [windows-packaging.md](./windows-packaging.md): the Microsoft Store counterpart.
<!--#endif-->
