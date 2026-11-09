---
title: Stop NBGV overriding the version CI injects on Android and iOS
impact: recommended
features: [packaging]
platforms: [android, ios]
---

## Why

Nerdbank.GitVersioning's `NBGV_SetVersionForMauiAndroid` and `NBGV_SetVersionForMauiIOS` targets set `ApplicationVersion`
and `ApplicationDisplayVersion` during the build. A property set inside a target beats even a `-p:` value, so the version
CI injected was silently replaced. On Android, a build meant to carry one `versionCode` came out as `65708`, and a store
rejects an upload whose code is too low.

## Who is affected

Apps with the Android or iOS head and the template's release workflows. Skip this if `src/Directory.Build.props` already
sets `NBGV_SetMauiVersionProperties`.

## Steps

`mzuno-app upgrade --patch` delivers this, except where you've changed that file.

Add this to `src/Directory.Build.props`, after the group that captures `_InjectedApplicationVersion`:

```xml
<!-- NBGV's NBGV_SetVersionForMauiAndroid/iOS targets overwrite ApplicationVersion and
     ApplicationDisplayVersion mid-build, even over -p: values. CI's injected version must win. -->
<PropertyGroup Condition="'$(CiVersionInjected)' == 'true'">
  <NBGV_SetMauiVersionProperties>false</NBGV_SetMauiVersionProperties>
</PropertyGroup>
```

Local builds don't set `CiVersionInjected`, so NBGV still derives their version as before.

If Play already holds a build with a higher `versionCode` than your pipeline now produces, set the `VERSION_CODE_OFFSET`
variable so new builds land above it. No patch can do that for you.

## Verify

Build with an injected version, as CI does:

```bash
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-android \
  -p:CiVersionInjected=true -p:ApplicationDisplayVersion=1.2.3 -p:ApplicationVersion=100003
```

Then open `src/AppTemplate/obj/Debug/net10.0-android/android/AndroidManifest.xml`. It should say
`android:versionCode="100003"` and `android:versionName="1.2.3"`, not a version NBGV derived.
