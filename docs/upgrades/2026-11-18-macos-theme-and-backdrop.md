---
title: Fix the macOS title bar theme and the stale frames left by Mica
impact: recommended
features: []
platforms: [desktop]
---

## Why

Two Uno Platform bugs hit the Desktop head on macOS:

- **Stale frames.** With a `SystemBackdrop` set, the window leaves earlier frames on screen: scrolling smears into doubled
  text, and switching pages leaves a ghost of the previous one
  ([uno#24495](https://github.com/unoplatform/uno/issues/24495)). The template sets `MicaBackdrop` in `WindowShell`, so
  every app had it.
- **Wrong title bar.** AppKit draws the title bar from the system appearance, not the app's theme, so a dark app on a
  light Mac gets a light title bar ([uno#24667](https://github.com/unoplatform/uno/issues/24667)).

The shell now skips the backdrop on macOS and hands its theme to AppKit. Both issues are open; drop each workaround once
its issue is fixed in the Uno.Sdk you use.

## Who is affected

Apps with the `desktop` platform. Windows, Linux and the other heads behave as before.

## Steps

`mzuno-app upgrade --patch` applies both. By hand:

1. Add [`src/AppTemplate/Infrastructure/MacAppearance.cs`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/AppTemplate/Infrastructure/MacAppearance.cs)
   to the app project. It sets `NSApp.appearance` through the Objective-C runtime and does nothing on other systems.

2. In `WindowShell.xaml.cs`, keep the backdrop off macOS. Before:

   ```csharp
   if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "SystemBackdrop"))
   {
       _associatedWindow.SystemBackdrop = new MicaBackdrop();
       Background = null;
   }
   ```

   After:

   ```csharp
   // With a backdrop set, the macOS desktop head leaves stale frames on screen while scrolling and navigating
   // (https://github.com/unoplatform/uno/issues/24495), so the page keeps its own background there.
   if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "SystemBackdrop") && !OperatingSystem.IsMacOS())
   {
       _associatedWindow.SystemBackdrop = new MicaBackdrop();
       Background = null;
   }
   ```

3. In the same file, apply the theme to AppKit whenever it's applied to the status bar. Before:

   ```csharp
   Loaded += (_, _) => UpdateStatusBarForeground();
   ActualThemeChanged += (_, _) => UpdateStatusBarForeground();
   ```

   After:

   ```csharp
   Loaded += (_, _) => ApplyThemeToSystemChrome();
   ActualThemeChanged += (_, _) => ApplyThemeToSystemChrome();
   ```

   ```csharp
   private void ApplyThemeToSystemChrome()
   {
       UpdateStatusBarForeground();
       MacAppearance.Apply(ActualTheme);
   }
   ```

   If your shell sets the backdrop or listens for theme changes somewhere else, make the same two changes there.

## Verify

`dotnet build src/AppTemplate/AppTemplate.csproj -f net11.0-desktop` succeeds. On a Mac, run the desktop head, switch the
app to the theme opposite to the system's: the title bar follows. Scrolling a long page and switching pages leaves no
trails.
