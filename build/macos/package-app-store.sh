#!/bin/bash
# Builds the Mac App Store package: a universal (arm64 + x64) sandboxed app bundle, signed and wrapped in an installer
# .pkg. Without a provisioning profile it stops at the unsigned universal bundle and zips it. See docs/mac-app-store.md.
#
# Usage: package-app-store.sh [Mac App Store provisioning profile]
# Environment:
#   APP_CHANNEL   Prod (default) or Dev
#   APP_VERSION   X.Y.Z to stamp into the bundle. Default: the Nerdbank.GitVersioning version
#   CODESIGN_KEY  the "Apple Distribution" identity (name or SHA-1). Default: "Apple Distribution"
#   INSTALLER_KEY the "3rd Party Mac Developer Installer" identity. Default: "3rd Party Mac Developer Installer"
# Writes artifacts/macos/AppTemplate.pkg, or artifacts/macos/AppTemplate.app.zip without a profile.
set -euo pipefail

PROFILE=${1:-}
APP_CHANNEL=${APP_CHANNEL:-Prod}
CODESIGN_KEY=${CODESIGN_KEY:-Apple Distribution}
INSTALLER_KEY=${INSTALLER_KEY:-3rd Party Mac Developer Installer}

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
PROJECT="$ROOT/src/AppTemplate/AppTemplate.csproj"
TFM=net10.0-desktop
BIN="$ROOT/src/AppTemplate/bin/Release/$TFM"
OUT="$ROOT/artifacts/macos"
rm -rf "$OUT"
mkdir -p "$OUT"

VERSION_ARGS=()
if [ -n "${APP_VERSION:-}" ]; then
  # ApplicationVersion=0 as on every desktop build; SetMacOSBundleVersion puts X.Y.Z into the bundle.
  VERSION_ARGS=(-p:CiVersionInjected=true "-p:Version=$APP_VERSION" "-p:ApplicationDisplayVersion=$APP_VERSION" -p:ApplicationVersion=0)
fi

# Only the desktop target, and self-contained at restore too, or restore misses the runtime packs.
# https://github.com/unoplatform/uno/issues/24663
for rid in osx-arm64 osx-x64; do
  rm -rf "$BIN/$rid/publish"
  dotnet publish "$PROJECT" -c Release -f $TFM -r "$rid" \
    -p:TargetFrameworks=$TFM -p:SelfContained=true -p:PackageFormat=app \
    "-p:AppChannel=$APP_CHANNEL" ${VERSION_ARGS[@]+"${VERSION_ARGS[@]}"}
done

APP_NAME=$(cd "$BIN/osx-arm64/publish" && find . -maxdepth 1 -name '*.app' -exec basename {} \; | head -1)
[ -n "$APP_NAME" ] || { echo "No app bundle in $BIN/osx-arm64/publish" >&2; exit 1; }

# Uno only imports its merge, sign and package targets while publishing, and the merge passes paths to lipo unquoted,
# so it merges copies without the spaces a display name may have. https://github.com/unoplatform/uno/issues/24665
UNO_TARGET=("-p:TargetFramework=$TFM" "-p:TargetFrameworks=$TFM" -p:_IsPublishing=true)
APP="$OUT/$APP_NAME"
MERGE="$OUT/merge"
mkdir -p "$MERGE/arm64" "$MERGE/x64"
cp -R "$BIN/osx-arm64/publish/$APP_NAME" "$MERGE/arm64/App.app"
cp -R "$BIN/osx-x64/publish/$APP_NAME" "$MERGE/x64/App.app"
dotnet msbuild "$PROJECT" -t:UnoMergeBundles "${UNO_TARGET[@]}" \
  "-p:UnoArm64Bundle=$MERGE/arm64/App.app" \
  "-p:UnoX64Bundle=$MERGE/x64/App.app" \
  "-p:UnoFatBundle=$MERGE/App.app"
mv "$MERGE/App.app" "$APP"
rm -rf "$MERGE"

# The merge moves the assemblies into Resources/.arm64 and .x64 but leaves Assets and the other files in Resources,
# and Uno resolves ms-appx:/// from the entry assembly's folder, so link them into both.
# https://github.com/unoplatform/uno/issues/24666
RESOURCES="$APP/Contents/Resources"
for arch in "$RESOURCES/.arm64" "$RESOURCES/.x64"; do
  for entry in "$RESOURCES"/*; do
    name=$(basename "$entry")
    case "$name" in
      *.dll|*.pdb) ;;
      *) [ -e "$arch/$name" ] || ln -s "../$name" "$arch/$name" ;;
    esac
  done
done

if [ -z "$PROFILE" ]; then
  (cd "$OUT" && ditto -c -k --keepParent "$APP_NAME" "AppTemplate.app.zip")
  echo "No provisioning profile, so the bundle is unsigned. Wrote $OUT/AppTemplate.app.zip"
  exit 0
fi

# Uno's tooling has no Mac App Store path, so the profile, its identifiers and the installer signature are added here.
# https://github.com/unoplatform/uno/issues/24668
cp "$PROFILE" "$APP/Contents/embedded.provisionprofile"

# The app's entitlements plus the identifiers from the profile, which the App Store checks against it.
ENTITLEMENTS="$OUT/Entitlements.plist"
PROFILE_PLIST="$OUT/profile.plist"
cp "$ROOT/src/AppTemplate/Platforms/Desktop/macOS/Entitlements.plist" "$ENTITLEMENTS"
security cms -D -i "$PROFILE" > "$PROFILE_PLIST"
for key in com.apple.application-identifier com.apple.developer.team-identifier; do
  value=$(/usr/libexec/PlistBuddy -c "Print :Entitlements:$key" "$PROFILE_PLIST")
  /usr/libexec/PlistBuddy -c "Add :$key string $value" "$ENTITLEMENTS"
done
rm "$PROFILE_PLIST"

dotnet msbuild "$PROJECT" -t:UnoSignAppBundle "${UNO_TARGET[@]}" \
  "-p:AppBundlePath=$APP" "-p:CodesignKey=$CODESIGN_KEY" "-p:UnoMacOSEntitlements=$ENTITLEMENTS"

dotnet msbuild "$PROJECT" -t:UnoPackageAppBundle "${UNO_TARGET[@]}" \
  "-p:AppBundlePath=$APP" "-p:PackageSigningKey=$INSTALLER_KEY"

PKG=$(find "$OUT" -maxdepth 1 -name '*.pkg' | head -1)
[ -n "$PKG" ] || { echo "UnoPackageAppBundle wrote no .pkg to $OUT" >&2; exit 1; }
[ "$PKG" = "$OUT/AppTemplate.pkg" ] || mv "$PKG" "$OUT/AppTemplate.pkg"
codesign --verify --strict --deep "$APP"
pkgutil --check-signature "$OUT/AppTemplate.pkg"
echo "Wrote $OUT/AppTemplate.pkg"
