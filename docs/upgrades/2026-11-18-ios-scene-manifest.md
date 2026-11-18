---
title: Declare a scene manifest for iOS 27
impact: breaking
features: []
platforms: [ios]
---

## Why

Apps built with the iOS 27 SDK (Xcode 27) must use the UIKit scene lifecycle, and the .NET iOS SDK now fails the build
when `Info.plist` doesn't declare one:

```text
Platforms/iOS/Info.plist : error MT7199: The app's Info.plist does not contain a UIApplicationSceneManifest dictionary.
```

Uno 7 runs a scene delegate for you either way, but only a declared manifest satisfies the build check.

## Who is affected

Apps with the iOS head that build with Xcode 27, which every CI build on the `xcode-27` image does.

If your app overrides `OpenUrl`, `ContinueUserActivity`, `PerformActionForShortcutItem`, `OnActivated` or
`DidEnterBackground` on a custom `UnoUIApplicationDelegate`, those stop being called once the manifest is declared. Read
those activations through `AppInstance` instead, as
[Uno's scene lifecycle docs](https://platform.uno/docs/articles/features/customizing-uiapplicationdelegate.html#scene-lifecycle)
describe.

## Steps

In `src/AppTemplate/Platforms/iOS/Info.plist`, add inside the top-level `<dict>`:

```xml
<key>UIApplicationSceneManifest</key>
<dict>
    <key>UIApplicationSupportsMultipleScenes</key>
    <false/>
    <key>UISceneConfigurations</key>
    <dict>
        <key>UIWindowSceneSessionRoleApplication</key>
        <array>
            <dict>
                <key>UISceneConfigurationName</key>
                <string>Default Configuration</string>
            </dict>
        </array>
    </dict>
</dict>
```

Leave out `UISceneDelegateClassName`, and Uno uses its own `UnoUISceneDelegate`.

## Verify

On a Mac with Xcode 27:

```bash
dotnet build src/AppTemplate/AppTemplate.csproj -c Release -f net11.0-ios
```

The build no longer reports `MT7199`. Launch the app on an iOS 27 simulator or device and check it opens its main
window.
