---
title: Google Play publishing guide, and two traps for a first release
impact: optional
features: [packaging]
platforms: [android]
---

## Why

There was no record of what publishing an app to Google Play takes beyond the CI workflows. The new
[google-play-publishing.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/google-play-publishing.md)
walks through the whole first publish, with the decisions and caveats found while publishing Merry Countdown. Two of
them can break a first release:

- Play only accepts **draft** releases for an app that has never been published, but `release.yml` uploads to the
  internal track with `status: completed`.
- Apps that use RevenueCat (`Kebechet.Maui.RevenueCat.Android`) get the `com.google.android.gms.permission.AD_ID`
  permission through `Xamarin.GooglePlayServices.Ads.Identifier`. That contradicts an "Advertising ID: No" declaration
  and Play's Families policy for kids' apps.

## Who is affected

Apps with the Android head that haven't been published on Google Play yet, or that use RevenueCat. Skip it if your app
is already live on Play and doesn't use RevenueCat.

## Steps

1. Copy `docs/google-play-publishing.md` from the template into your app's `docs/`, and add its line to `docs/README.md`
   under Release.
2. Before the first Play publish, make the first internal upload a draft: change `status: completed` to `status: draft`
   in `.github/workflows/release.yml` (`publish-play-internal`) for that release, or upload the first bundle in Play
   Console by hand. Switch back once the app is published.
3. If your app uses RevenueCat, remove the advertising ID permission in
   `src/AppTemplate/Platforms/Android/AndroidManifest.xml`:

   ```xml
   <manifest xmlns:android="http://schemas.android.com/apk/res/android" xmlns:tools="http://schemas.android.com/tools">
     <uses-permission android:name="com.google.android.gms.permission.AD_ID" tools:node="remove" />
   </manifest>
   ```

   (Merge the attribute and element into your existing manifest rather than replacing it.)

Everything else in the guide is store-console work that a patch can't deliver.

## Verify

`aapt dump permissions <your.apk>` lists no `com.google.android.gms.permission.AD_ID`, and the first
`publish-play-internal` run on a new app succeeds.
