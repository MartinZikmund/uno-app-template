# Publishing to the App Store

Everything it takes to get an app from this template onto the App Store the first time: certificates and keys, the
toolchain traps, the listing, the in-app purchase, RevenueCat, App Privacy, the Kids category and App Review. The CI
side (what `release.yml` builds and uploads) is in [release-pipeline.md](./release-pipeline.md); this page is the App
Store Connect side and the decisions around it.

It was written while publishing Merry Countdown (a family app for young children, with one non-consumable in-app purchase) in November
2026. Values from that app are shown as examples. Each step says who has to do it:

- **Agent**: an agent driving the browser, or the Mac's command line, can do it.
- **Human**: needs the account owner, because it accepts terms, attests to something legally, types a password, or
  the browser automation's permission guard refuses it. Hand these over as one-click tasks with the form already filled.

## Checklist

For agents: work through this in order. Each line links to its section.

1. [ ] [Check the developer account](#1-check-the-developer-account): membership, agreements, banking (**Human**).
2. [ ] [Create the app record](#2-create-the-app-record): bundle ID, SKU, primary language.
3. [ ] [Certificate, profile and API keys](#3-certificate-profile-and-api-keys), then the GitHub secrets.
4. [ ] [Toolchain checks](#4-toolchain-checks) before the first upload: Xcode, SDK, scene and privacy manifests.
5. [ ] [First TestFlight build](#5-first-testflight-build).
6. [ ] [Store listing](#6-store-listing) text and screenshots, per language.
7. [ ] [Pricing and availability](#7-pricing-and-availability), including Mac and China.
8. [ ] [In-app purchase](#8-in-app-purchase), price and sale.
9. [ ] [RevenueCat](#9-revenuecat).
10. [ ] [App Privacy](#10-app-privacy) (publishing is **Human**).
11. [ ] [Age rating and the Kids category](#11-age-rating-and-the-kids-category).
12. [ ] [App Review information](#12-app-review-information).
13. [ ] [TestFlight and sandbox testing](#13-testflight-and-sandbox-testing).
14. [ ] [Submit for review](#14-submit-for-review).

## 1. Check the developer account

**Human.** Apple Developer Program membership must be active, and in App Store Connect → Business the **Paid Apps
agreement**, tax forms and banking must be complete before an in-app purchase can be sold (a free app without
purchases only needs the free agreement). Agreements can't be accepted by an agent.

## 2. Create the app record

App Store Connect → Apps → *New App*. **Agent**, if the account owner is signed in.

- **Bundle ID**: the Prod `ApplicationId`. Register it first in the
  Developer portal → Identifiers. It can never change, and a later native macOS build only joins the same listing
  (universal purchase) if it uses the same bundle ID.
- **SKU**: anything unique to you, for example the app name in lowercase.
- **Primary language**: the language the listing falls back to. Other languages are added per version later.
- The version App Store Connect creates (`1.0`) must match the build's `CFBundleShortVersionString`. With
  Nerdbank.GitVersioning that's `major.minor.height` (for example `0.3.34`), so rename the App Store Connect version to
  the build you submit, not the other way round.

## 3. Certificate, profile and API keys

What's per team and what's per app:

| Item | Scope | Used by |
| --- | --- | --- |
| Apple Distribution certificate (+ private key, `.p12`) | team, max 3 at a time | CI signing: `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD`, `APPLE_CODESIGN_KEY` |
| App Store provisioning profile | per app | CI signing: `APPLE_PROVISIONING_PROFILE_BASE64`, `vars.APPLE_PROVISIONING_PROFILE_NAME` |
| App Store Connect API key (`.p8`, App Manager) | team | TestFlight upload and review submission: `APPSTORE_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_PRIVATE_KEY` |
| In-App Purchase key (`.p8`) | team | RevenueCat only |

- **Decision: reuse the team's distribution certificate.** If one with a private key exists in a keychain you can
  reach, export that. Otherwise create a new one; certificates expire after a year, and editing the profile to add a
  new certificate regenerates its UUID (which is why the template refers to the profile by **name**).
- **The `.p12` must hold the certificate's own private key.** Export from Keychain Access → *My Certificates* with the
  certificate row selected. A key that doesn't belong to the certificate imports fine and then fails at build time with
  *"iOS code signing key not found in keychain"*. Check before uploading: these two must print the same hash.

  ```bash
  openssl pkcs12 -in dist.p12 -nokeys -legacy | openssl x509 -noout -pubkey | openssl sha1
  openssl pkcs12 -in dist.p12 -nocerts -nodes -legacy | openssl pkey -pubout | openssl sha1
  ```

- **Making the certificate without a Mac** (openssl key and CSR, upload the CSR in the portal):
  - Build the `.p12` with `-keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1`. macOS `security import` rejects
    the newer AES/SHA-256 defaults, and OpenSSL 1.1.1 puts the certificate in an RC2-40 bag, both with the misleading
    error *"The user name or passphrase you entered is not correct"*.
  - On Git Bash, prefix `openssl req -subj "/CN=…"` with `MSYS_NO_PATHCONV=1`, or MSYS rewrites the subject as a path.
  - MinGW's `openssl rand` writes CRLF. Strip both `\r` and `\n` from a generated password, or the stray `\r` ends up
    in `APPLE_P12_PASSWORD` and the import fails with the same "passphrase" error.
- **Keys download once.** Move each `.p8` and `.p12` straight into the password manager, and set the GitHub secrets
  from files (`gh secret set NAME < file`) or let the owner paste them, never echoing them into a transcript.
- The profile: Developer portal → Profiles → *App Store Connect* type → the App ID → the certificate. Its name goes in
  the `APPLE_PROVISIONING_PROFILE_NAME` repository variable.

## 4. Toolchain checks

Do these before the first upload; each one cost Merry Countdown a build.

- **Build with the current iOS SDK.** App Store Connect warns (ITMS-90725) about uploads built with an older SDK and
  turns it into a rejection after Apple's deadline. The template's `_build-ios.yml` runs on the `xcode-27` image, and
  `.github/actions/select-xcode` selects the exact Xcode the installed iOS workload asks for and points
  `MD_APPLE_SDK_ROOT` at it (the .NET iOS SDK reads that variable before `xcode-select`, and runner images set it).
  Check the uploaded `.ipa`'s `Info.plist`: `DTSDKName` and `DTXcode` must be the current major version.
- **Pin the .NET SDK** in `global.json`. A runner image with a newer SDK preinstalled builds your target framework
  with that SDK's workload, which can carry an *older* iOS pack for your TFM, so the build silently comes out on an old
  iOS SDK. Keep `ValidateXcodeVersion` on: turning it off hides exactly this.
- **Scene manifest.** The iOS 27 SDK requires the UIKit scene lifecycle; the build fails with `MT7199` without a
  `UIApplicationSceneManifest` in `Info.plist`. The template declares one.
- **Privacy manifest.** `PrivacyInfo.xcprivacy` must be a valid plist (`plutil -lint`), with the required-reason APIs
  inside `NSPrivacyAccessedAPITypes`. A library that collects data, such as the RevenueCat SDK, ships its own manifest.
- **Export compliance.** If the app only uses the system's HTTPS, uncomment `ITSAppUsesNonExemptEncryption` = `false`
  in `Info.plist`, so App Store Connect doesn't ask about encryption on every build.

## 5. First TestFlight build

`release.yml`'s *publish-testflight* uploads the signed `.ipa` with the App Store Connect API key. Unlike Google Play,
the first build of a new app can go through the API. Builds appear in App Store Connect after processing (5–30
minutes), and Apple emails any warnings (such as ITMS-90725). Ignore stray builds from cancelled runs; you choose the
build per version.

## 6. Store listing

App Store Connect → the app → the version page, per language (the language picker sits at the top of each section).

### Text

| Field | Limit | Notes |
| --- | --- | --- |
| Name | 30 | Per language. Merry Countdown is *Čekání na Vánoce* in Czech. |
| Subtitle | 30 | |
| Promotional text | 170 | The only text you can change without a new review. |
| Description | 4000 | Drop platform-specific sentences (Merry Countdown's Windows-only mini window). |
| Keywords | 100 | Comma-separated, no spaces. Skip words already in the name and subtitle; Apple indexes those anyway. |
| Support URL | required | A web page, not an email address. Set it per language. |
| Privacy policy URL | required | App Privacy section, per language. |

### Screenshots

- **iPhone**: one 6.9" set (1320 × 2868) covers every smaller iPhone. Upload it into the *large display* (6.5"–6.9")
  slot, not the required *medium display* slot; medium and smaller then show *Using existing assets*.
- **iPad**: one 13" set (2064 × 2752) covers every iPad.
- Up to 10 per size; the order is the upload order. Upload one file at a time.
- A second language falls back to the primary language's screenshots until you upload its own into that locale.
- Simulator captures: `xcrun simctl status_bar booted override --time 9:41 --batteryState charged --batteryLevel 100`
  for a clean status bar. The iPad status bar also shows the real date and the app's name in the *device's* language;
  neither can be overridden.

## 7. Pricing and availability

- **Price**: *Add Pricing* → $0.00 for a free app with an in-app purchase.
- **China mainland**: excluded for Merry Countdown. Publishing there needs an ICP filing, a security assessment and,
  for anything generative, an AIGC declaration, which a foreign individual can't realistically get. Hong Kong, Macau
  and Taiwan are separate regions and stay available (174 of 175).
- **Apple Silicon Macs**: the unmodified iPhone/iPad app is offered on M-series Macs, and it's **on by default**. Test
  it (TestFlight runs on the Mac too) or switch it off. Window resizing, mouse and hardware keyboard are the parts the
  Simulator never exercised.
- **Apple Vision Pro**: an app *Made for Kids* can't be offered there.

## 8. In-app purchase

App Store Connect → the app → *In-App Purchases*.

- **Type**: non-consumable for a one-time unlock. The **Product ID** is permanent and must match the app's code and
  RevenueCat.
- **Limits**: display name 30, description 55 characters, per language.
- **Price points**: the list only shows `.99` tiers until *See Additional Prices* (Merry Countdown's US$1.79 is there).
- **Launch sale**: Apple has no discount offers for non-consumables. Set the sale price as the starting price and
  *Schedule a price change* (Global → effective date) back to the full price. Apple warns about conflicts with its own
  currency and tax adjustments; continuing is fine. In-app purchase prices only take effect once the app is *Ready for
  Sale*, so a sale that ends before approval never happens.
- **Family Sharing**: decide deliberately. Once turned on, it **can't be turned off**.
- **Review screenshot**: a capture of the screen where the purchase is offered, with its price button. *Add for
  Review* only becomes available once the screenshot and every localization exist.
- **Decision**: keep the product's name the same as the in-app wording. Merry Countdown's app says "Full Version"
  while the store says "Pro", so the review notes have to bridge the two.

## 9. RevenueCat

The template doesn't include a billing library. Merry Countdown sells through RevenueCat; if your app does too:

**Agent**, once the owner has logged in.

1. In the project that already has the Play app (or a new one): *Apps* → *New app configuration* → *App Store*. A
   banner saying *"You don't have permission to add app configurations"* is wrong; the button works.
2. Bundle ID, the **In-App Purchase key** (`.p8`; uploading it fills the Key ID) and the issuer ID. The App Store
   Connect API key is optional here.
3. *Product catalog* → *Products* → *New product*: the App Store product ID, attached to the same entitlement as the
   Play product.
4. Add it to the **same package** of the current offering as the Play product (Merry Countdown's store service sells
   `GetCurrent().AvailablePackages.First()`).
5. *API keys* → the public `appl_…` SDK key goes into the app's store configuration. Public keys are designed to ship
   inside apps; secret keys never do.

## 10. App Privacy

App Store Connect → the app → *App Privacy*. The labels must match the privacy policy word for word in spirit, and
include at least what the bundled privacy manifests declare.

| Data | Merry Countdown's answer |
| --- | --- |
| Purchase History | App Functionality; not linked to identity; no tracking (RevenueCat's own manifest declares exactly this) |
| User ID | App Functionality; not linked; no tracking (RevenueCat's anonymous app user ID; a superset is allowed) |
| Everything else | not collected; nothing else leaves the device |

**Publishing** the answers attests that they're accurate: **Human**.

## 11. Age rating and the Kids category

- **Age rating**: answer the questionnaire honestly (Merry Countdown: 4+).
- **Decision: the Kids category or not.** *Made for Kids* is **permanent** once the app is approved, and it rules out
  third-party analytics and advertising for good. Weigh where the app might go, not only where it is. Merry Countdown
  first chose Kids (5 and under), then left it because ads may come later: it's rated 4+ with the age category
  *Not Applicable*, under Lifestyle and Entertainment.
- A Kids submission gets an automated **guideline 1.3 questionnaire** (third-party analytics, advertising, data
  sharing, other data use) in the Resolution Center, and review pauses until it's answered.
- If you do choose Kids, the age band (5 and under, 6–8, 9–11) shows on the store; pick it deliberately (Merry
  Countdown nearly shipped as 6–8 by mistake). Every purchase and every link out of the app must sit behind a
  **parental gate**. The template doesn't include one; Merry Countdown keeps its own (a multiplication question
  written in words) outside the Kids category too.

## 12. App Review information

The notes are what a reviewer reads first, and for an app aimed at families they decide the review. Merry Countdown's
covered:

- **The parental gate**, in words, and **every** link out of the app it guards (videos, sharing). Don't list links the
  app doesn't have: a leftover rating prompt that's never called isn't one.
- **Any parent-chosen bypass** of the gate (Merry Countdown's opt-in "keep this corner unlocked", off by default), so
  the reviewer doesn't find it first.
- **The in-app purchase path**: where it's offered, what it unlocks, where *Restore purchase* is, and the in-app name
  if it differs from the store's.
- **Seasonal or date-gated content**: tell the reviewer to change the **device date** (Settings → General → Date &
  Time → turn off *Set Automatically*), and to a date in the current season. Don't add review-only behaviour to the
  app: Merry Countdown built a "sneak peek" door for reviewers and reverted it.
- **Sign-in**: say none is needed if so. The **contact** (name, phone, email) is the owner's: **Human**.

## 13. TestFlight and sandbox testing

- *TestFlight* → *Internal Testing* → a group with **automatic distribution**, with the owner in it. Internal builds
  aren't reviewed.
- Sandbox purchases need a **sandbox tester** (Users and Access → Sandbox → Test Accounts): an email that isn't a real
  Apple ID. On the device, sign in under Settings → Developer (or App Store) → Sandbox Account. The purchase sheet says
  *[Environment: Sandbox]*; sandbox purchases are free and can be cleared per tester.
- Check: the real price shows, buying unlocks, deleting and reinstalling then *Restore purchase* brings it back, and
  the silent switch silences the app's sounds.

## 14. Submit for review

On the version page: choose the build, check every section shows no warning, and choose the release option (manual
release lets you pick the day).

- **The first in-app purchase must be in the same submission as the version.** Use the purchase's own *Add for Review*
  first, then the version's *Add for Review*, so the draft submission lists both ("2 items"), then *Submit*. A version
  submitted on its own goes to review without the purchase, and the purchase can't follow it in later.
- If that already happened: remove the version from that submission (*Remove last item with issues* once it's paused
  or rejected), then add the purchase and the version to one new draft and submit them together.
- Review of a new app can take days, and a paused review (such as the Kids questionnaire) waits for you. Leave a margin
  before any date that matters.

## Automating App Store Connect

Notes for agents driving App Store Connect in a browser.

- The forms are React. The native value setter plus `input`/`change` events works on some fields (Promotional Text,
  Description, Keywords, Copyright) and silently not on others (review Notes, the privacy policy URL dialog, the
  in-app purchase localization dialog): *Save* stays disabled. Real typing (click, then type) or the extension's form
  input works everywhere. Always check that *Save* turned enabled, and disabled again after saving.
- A password manager extension can intercept clicks near the App Review sign-in fields (*"Cannot access a
  chrome-extension:// URL"*); use form input there.
- Every field is per language, including the Support URL and the privacy policy URL; the language picker is at the top
  of each section (version page, App Privacy, Media Manager).
- Media Manager: the hidden file input is replaced after every upload, so find it again each time. A one-time info
  dialog appears after the first iPad upload and needs *OK*.
- **Human only**: publishing App Privacy, accepting agreements, the review contact, and anything typed into a password
  field. Fill the form, then hand over one click.

## Related

- [release-pipeline.md](./release-pipeline.md): the workflows, environments and secrets.
- [release-runbook.md](./release-runbook.md): releases, patches, halting.
- [versioning.md](./versioning.md): how the version and build number are computed.
<!--#if (hasAndroid)-->
- [google-play-publishing.md](./google-play-publishing.md): the Google Play counterpart.
<!--#endif-->
