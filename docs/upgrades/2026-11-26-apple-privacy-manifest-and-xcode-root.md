---
title: Make the Apple privacy manifest a valid plist, and point MD_APPLE_SDK_ROOT at the selected Xcode
impact: recommended
features: []
platforms: [ios, tvos]
---

## Why

Two problems found while publishing an app from this template to the App Store:

- `PrivacyInfo.xcprivacy` wasn't a valid plist. Its four `<dict>` entries sat directly under `<plist>`, without the
  `NSPrivacyAccessedAPITypes` array around them, so the bundled manifest didn't parse (`plutil -lint` fails with
  "plist can only include one object"). The build doesn't check it; App Store Connect can reject an invalid one.
- `select-xcode` switched Xcode with `xcode-select`, but the .NET iOS SDK reads `MD_APPLE_SDK_ROOT` first, and runner
  images set it to their default Xcode. The build could then use a different Xcode, and iOS SDK, than the one selected.

## Who is affected

Every app with the iOS or tvOS head. The second point only matters for apps that build them in CI with
`.github/actions/select-xcode`.

## Steps

1. Replace `src/AppTemplate/Platforms/iOS/PrivacyInfo.xcprivacy` (and `Platforms/tvOS/PrivacyInfo.xcprivacy`, if you
   have it) with the template's
   [iOS manifest](https://github.com/MartinZikmund/uno-app-template/blob/main/src/AppTemplate/Platforms/iOS/PrivacyInfo.xcprivacy).
   If you added entries of your own, keep them, moved inside `NSPrivacyAccessedAPITypes`. The shape is:

   ```xml
   <plist version="1.0">
   <dict>
       <key>NSPrivacyTracking</key>
       <false/>
       <key>NSPrivacyTrackingDomains</key>
       <array/>
       <key>NSPrivacyCollectedDataTypes</key>
       <array/>
       <key>NSPrivacyAccessedAPITypes</key>
       <array>
           <dict>
               <key>NSPrivacyAccessedAPIType</key>
               <string>NSPrivacyAccessedAPICategoryFileTimestamp</string>
               <key>NSPrivacyAccessedAPITypeReasons</key>
               <array>
                   <string>C617.1</string>
               </array>
           </dict>
           <!-- SystemBootTime 35F9.1, DiskSpace E174.1, UserDefaults CA92.1 the same way -->
       </array>
   </dict>
   </plist>
   ```

2. Copy
   [`tests/AppTemplate.Core.Tests/Packaging/PrivacyManifestTests.cs`](https://github.com/MartinZikmund/uno-app-template/blob/main/tests/AppTemplate.Core.Tests/Packaging/PrivacyManifestTests.cs)
   into your test project, so a broken manifest fails a test.

3. In `.github/actions/select-xcode/action.yml`, export the selected Xcode right after `xcode-select`:

   ```diff
                sudo xcode-select -s "$app"
   +            # The .NET iOS SDK reads MD_APPLE_SDK_ROOT before xcode-select, and runner images set it.
   +            echo "MD_APPLE_SDK_ROOT=$app" >> "$GITHUB_ENV"
                xcodebuild -version
   ```

## Verify

`plutil -lint src/AppTemplate/Platforms/iOS/PrivacyInfo.xcprivacy` prints `OK`, and the new test passes. In a CI
build, the `.ipa`'s `Info.plist` has `DTXcode` matching the Xcode that **Select Xcode** printed.
