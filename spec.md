# Uno Platform App Template - Enhancement Specification

This specification outlines features, patterns, and code to integrate into the reusable Uno Platform app template based on analysis of the **Stopwatch** and **Awaitick** applications.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Template State](#current-template-state)
3. [Features to Integrate](#features-to-integrate)
4. [Architecture Improvements](#architecture-improvements)
5. [UI Components & Styling](#ui-components--styling)
6. [Services to Add](#services-to-add)
7. [Platform-Specific Enhancements](#platform-specific-enhancements)
8. [MZikmund.Toolkit Extractions](#mzikmundtoolkit-extractions)
9. [Implementation Priority](#implementation-priority)

---

## Executive Summary

The current template provides a solid foundation with hosting, DI, basic navigation, localization, and HTTP client support. However, both production apps (Stopwatch and Awaitick) share many common patterns and services that should be extracted into the template for maximum reusability.

### Key Findings

| Area | Current Template | Stopwatch | Awaitick | Recommendation |
|------|------------------|-----------|----------|----------------|
| Navigation | Basic frame nav | Convention-based ViewModel routing | Same pattern | Upgrade to convention-based |
| Theming | Placeholder only | Full ThemeManager with title bar | Same | Add complete ThemeManager |
| Settings | None | IAppPreferences wrapper | Same | Add settings infrastructure |
| Dialogs | None | IDialogService + coordination | Same | Add dialog infrastructure |
| Data | HTTP only | LiteDB/File abstraction | JSON file service | Add data persistence pattern |
| Store/IAP | None | RevenueCat/MS Store | Plugin.InAppBilling | Add abstraction layer |
| Converters | None | Rich converter library | 8+ converters | Add comprehensive converter set |

---

## Current Template State

### Already Implemented ✅
- Multi-platform support (Windows, Android, iOS, WebAssembly, Desktop/Linux)
- Dependency injection via Microsoft.Extensions.Hosting
- Basic INavigationService with ViewModel-to-View mapping
- Configuration via appsettings.json (environment-aware)
- Localization infrastructure (en, cs)
- HTTP client via Refit with debug handler
- Window shell with title bar and back navigation
- Mica backdrop (Windows)

### Missing/Incomplete ❌
- WindowShellProvider (referenced but not implemented)
- WindowShellViewModel (referenced but not implemented)
- IThemeManager (referenced but not implemented)
- IAppPreferences (referenced but not implemented)
- Dialog infrastructure
- Converter library
- Settings/preferences persistence
- Onboarding framework
- Store/purchase abstraction

---

## Features to Integrate

### 1. Navigation Service Enhancement

**Source:** Both apps use identical pattern

```
Current: Manual view registration
Target: Convention-based auto-discovery
```

**Changes Required:**
- Auto-scan assembly for View types matching `*View` pattern
- Map ViewModels to Views by convention (`MainViewModel` → `MainView`)
- Support navigation parameters via generic `NavigationModel<T>`
- Add `IFrameProvider` for multi-frame scenarios
- Add deep link support via `IDeepLinkService`

**Files to Create:**
```
Services/Navigation/
├── INavigationService.cs (enhance existing)
├── NavigationService.cs (enhance existing)
├── IFrameProvider.cs
├── FrameProvider.cs
├── IDeepLinkService.cs
├── DeepLinkService.cs
└── NavigationModel.cs
```

### 2. Theme Management

**Source:** Both apps have nearly identical ThemeManager

**Features:**
- Runtime theme switching (Light/Dark/Default)
- Title bar color customization per theme
- UISettings color change monitoring
- Persistent theme preference
- Per-entity theme support (optional)

**Files to Create:**
```
Services/Theming/
├── IThemeManager.cs
└── ThemeManager.cs
```

**Interface:**
```csharp
public interface IThemeManager
{
    ElementTheme CurrentTheme { get; }
    ElementTheme ActualTheme { get; }
    void SetTheme(ElementTheme theme);
    void ApplyTheme();
    event EventHandler<ElementTheme>? ThemeChanged;
}
```

### 3. App Preferences/Settings

**Source:** Both apps wrap IPreferences from MZikmund.Toolkit.WinUI

**Features:**
- Strongly-typed settings wrapper
- Default value management
- Complex type serialization (JSON)
- Debug reset capability
- Common settings: Theme, FirstStart, LaunchCount, OnboardingCompleted

**Files to Create:**
```
Services/Settings/
├── IAppPreferences.cs
└── AppPreferences.cs
```

**Base Interface:**
```csharp
public interface IAppPreferences
{
    // Core settings every app needs
    ElementTheme Theme { get; set; }
    bool IsFirstStart { get; set; }
    int LaunchCount { get; set; }
    bool OnboardingCompleted { get; set; }
    bool KeepScreenOn { get; set; }

    // Reset for debugging
    void Clear();
}
```

### 4. Dialog Infrastructure

**Source:** Both apps use IDialogService with XamlRoot handling

**Features:**
- Generic dialog display with ViewModel mapping
- XamlRoot provider for proper dialog positioning
- Dialog queue management (prevent stacking)
- Confirmation dialog helper
- Loading indicator coordination

**Files to Create:**
```
Services/Dialogs/
├── IDialogService.cs
├── DialogService.cs
├── IConfirmationDialogService.cs
├── ConfirmationDialogService.cs
├── IXamlRootProvider.cs
└── XamlRootProvider.cs
```

### 5. Data Persistence Layer

**Source:**
- Stopwatch: LiteDB (Windows/Desktop) + File (iOS/Android)
- Awaitick: JSON file service

**Recommendation:** Provide both patterns as options

**Files to Create:**
```
Services/Data/
├── IDataService.cs
├── FileDataService.cs (JSON-based, simple)
├── IRepository.cs (generic CRUD)
└── JsonSerializerContextBase.cs
```

**Features:**
- Async CRUD operations
- JSON serialization with source-generated contexts (AOT-safe)
- Platform-appropriate storage location
- Error handling with fallback

### 6. Display Request (Keep Screen On)

**Source:** Stopwatch has IDisplayRequestManager

**Files to Create:**
```
Services/Display/
├── IDisplayRequestManager.cs
└── DisplayRequestManager.cs
```

### 7. Localization Enhancements

**Source:** Both apps have Localizer static wrapper

**Features:**
- Static facade for easy access: `Localizer.Get("Key")`
- Enum localization support
- Fallback display (`???Key???`)
- XAML markup extension

**Files to Create:**
```
Services/Localization/
├── Localizer.cs (static facade)
└── LocalizeExtension.cs (XAML markup)
```

### 8. Window Shell Completion

**Source:** Both apps have complete WindowShell implementations

**Features:**
- WindowShellProvider for multi-window support
- WindowShellViewModel with back button state, title
- Service scope per window
- DispatcherQueue access

**Files to Create:**
```
Shell/
├── IWindowShell.cs (exists)
├── WindowShell.xaml (exists)
├── WindowShell.xaml.cs (enhance)
├── WindowShellViewModel.cs
├── IWindowShellProvider.cs
└── WindowShellProvider.cs
```

### 9. Onboarding Framework

**Source:** Both apps have onboarding flows

**Features:**
- FlipView-based multi-step wizard (Awaitick)
- Teaching tips for feature discovery (Stopwatch)
- First-run detection
- Skippable/completable state tracking

**Files to Create:**
```
Features/Onboarding/
├── IOnboardingService.cs
├── OnboardingService.cs
├── OnboardingView.xaml (template)
└── OnboardingViewModel.cs (template)
```

### 10. Store/In-App Purchase Abstraction

**Source:**
- Stopwatch: RevenueCat (iOS/Android), MS Store (Windows)
- Awaitick: Plugin.InAppBilling

**Files to Create:**
```
Services/Store/
├── IStoreService.cs
├── StoreService.cs (Windows)
├── FakeStoreService.cs (Debug)
└── RevenueCatStoreService.cs (optional, iOS/Android)
```

**Interface:**
```csharp
public interface IStoreService
{
    Task<bool> HasProAsync();
    Task<bool> PurchaseProAsync();
    Task<IEnumerable<ProductInfo>> GetProductsAsync();
}
```

---

## Architecture Improvements

### 1. ViewModel Base Classes

**Create hierarchy matching both apps:**

```csharp
// Base with common functionality
public abstract class ViewModelBase : ObservableRecipient
{
    protected INavigationService Navigation { get; }
    protected IDialogService Dialogs { get; }

    protected ViewModelBase()
    {
        Navigation = IoC.GetRequiredService<INavigationService>();
        Dialogs = IoC.GetRequiredService<IDialogService>();
    }
}

// Page-specific with lifecycle
public abstract class PageViewModel : ViewModelBase
{
    public virtual void OnNavigatedTo(object? parameter) { }
    public virtual void OnNavigatedFrom() { }
    public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask;
}
```

### 2. IoC Static Container

**Both apps use this pattern for easy service access:**

```csharp
public static class IoC
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void Initialize(IServiceProvider services) => Services = services;

    public static T GetRequiredService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    public static T? GetService<T>() => Services.GetService<T>();
}
```

### 3. Messaging Infrastructure

**Both apps use MVVM Toolkit's WeakReferenceMessenger:**

```csharp
// Base messages to include
public record NavigationChangedMessage(string? PageName);
public record ThemeChangedMessage(ElementTheme Theme);
public record DataChangedMessage<T>(T Data, ChangeType Type);

public enum ChangeType { Added, Updated, Deleted }
```

### 4. Service Registration Pattern

**Standardize registration in App.xaml.cs:**

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    // Core services (always)
    services.AddSingleton<INavigationService, NavigationService>();
    services.AddSingleton<IThemeManager, ThemeManager>();
    services.AddSingleton<IAppPreferences, AppPreferences>();
    services.AddSingleton<IDialogService, DialogService>();

    // Platform-conditional
    #if WINDOWS
    services.AddSingleton<IStoreService, StoreService>();
    #elif __IOS__ || __ANDROID__
    services.AddSingleton<IStoreService, RevenueCatStoreService>();
    #else
    services.AddSingleton<IStoreService, FakeStoreService>();
    #endif

    // ViewModels (scoped for navigation)
    services.AddTransient<MainViewModel>();
    services.AddTransient<SettingsViewModel>();
}
```

---

## UI Components & Styling

### 1. Converter Library

**Common converters from both apps to include:**

| Converter | Purpose | Source |
|-----------|---------|--------|
| `BoolToVisibilityConverter` | Bool ↔ Visibility with inversion | Both |
| `BoolNegationConverter` | Invert boolean | Awaitick |
| `BoolToObjectConverter` | Bool → any object | Stopwatch |
| `EmptyStringToVisibilityConverter` | String empty check | Awaitick |
| `NonEmptyStringToVisibilityConverter` | String presence check | Awaitick |
| `NonNullToVisibilityConverter` | Null check | Awaitick |
| `CountToVisibilityConverter` | Collection count check | Stopwatch |
| `EnumLocalizationConverter` | Enum → localized string | Both |
| `IntToTimeComponentStringConverter` | Zero-padded time | Awaitick |
| `ItemClickEventArgsConverter` | Event args helper | Awaitick |

**Files to Create:**
```
Converters/
├── BoolToVisibilityConverter.cs
├── BoolNegationConverter.cs
├── BoolToObjectConverter.cs
├── StringToVisibilityConverters.cs
├── NullToVisibilityConverter.cs
├── CountToVisibilityConverter.cs
├── EnumLocalizationConverter.cs
└── Converters.xaml (ResourceDictionary)
```

### 2. Style Templates

**Base styles from both apps:**

```
Resources/
├── Colors.xaml          - Brand colors, theme-aware brushes
├── Styles.xaml          - Button styles, text styles
├── Values.xaml          - Shared dimensions (TitleBarHeight, etc.)
├── Icons.xaml           - Common FontIcon definitions
├── Converters.xaml      - Converter references
└── DataTemplates.xaml   - Common item templates
```

**Button Styles to Include:**
- `SubtleButtonStyle` - Transparent with hover
- `AccentButtonStyle` - Filled accent color
- `NavigationBackButtonStyle` - Back arrow with animation
- `IconButtonStyle` - Icon-only button

### 3. Responsive Layout Patterns

**Breakpoints used consistently:**

```xaml
<!-- Standard breakpoints -->
<VisualState x:Name="Narrow">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />
    </VisualState.StateTriggers>
</VisualState>

<VisualState x:Name="Medium">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="640" />
    </VisualState.StateTriggers>
</VisualState>

<VisualState x:Name="Wide">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="1024" />
    </VisualState.StateTriggers>
</VisualState>
```

### 4. Common Controls

**Reusable controls to include:**

```
Controls/
├── PageHeaderControl.xaml     - Consistent page titles
├── LoadingOverlay.xaml        - Full-page loading indicator
└── EmptyStateControl.xaml     - Empty list placeholder
```

---

## Services to Add

### Complete Service Inventory

| Service | Interface | Implementation | Priority |
|---------|-----------|----------------|----------|
| Navigation | INavigationService | NavigationService | P0 |
| Theming | IThemeManager | ThemeManager | P0 |
| Preferences | IAppPreferences | AppPreferences | P0 |
| Dialogs | IDialogService | DialogService | P0 |
| Window Shell | IWindowShellProvider | WindowShellProvider | P0 |
| Localization | Localizer (static) | - | P1 |
| Data | IDataService | FileDataService | P1 |
| Display | IDisplayRequestManager | DisplayRequestManager | P2 |
| Store | IStoreService | StoreService | P2 |
| Sharing | ISystemSharingService | SystemSharingService | P2 |
| Mail | IMailService | MailService | P3 |
| Rating | IAppRatingService | AppRatingService | P3 |
| Notifications | INotificationService | NotificationService | P3 |

---

## Platform-Specific Enhancements

### 1. Android

**From both apps:**
- SplashScreen activity with branding
- Boot receiver for notification persistence
- AlarmManager for scheduled notifications
- Intent handling for deep links

**Files:**
```
Platforms/Android/
├── MainActivity.Android.cs (enhance)
├── BootReceiver.cs (optional)
└── NotificationReceiver.cs (optional)
```

### 2. iOS

**From both apps:**
- UNUserNotificationCenter delegate
- Local notification scheduling
- Background fetch support

**Files:**
```
Platforms/iOS/
├── Main.iOS.cs (exists)
├── NotificationDelegate.cs (optional)
└── Info.plist entries
```

### 3. Windows

**From both apps:**
- AppWindow customization
- Title bar extension
- Mica/Acrylic backdrop
- Toast notifications
- Multi-instance handling

**Files:**
```
Platforms/Desktop/
├── Program.cs (exists)
└── WindowHelper.cs (new)
```

### 4. WebAssembly

**From both apps:**
- IndexedDB persistence (IDBFS)
- PWA manifest
- Service worker for offline

**Files:**
```
Platforms/WebAssembly/
├── Program.cs (exists)
├── manifest.webmanifest
└── service-worker.js (optional)
```

---

## MZikmund.Toolkit Extractions

Based on analysis of both apps, the following utilities should be extracted to **MZikmund.Toolkit** for maximum reusability across projects:

### 1. MZikmund.Toolkit.Core (netstandard2.0)

**General utilities not tied to UI:**

```csharp
// Already referenced but could be enhanced
namespace MZikmund.Toolkit.Core
{
    // Extensions
    public static class StringExtensions { }
    public static class CollectionExtensions { }
    public static class TaskExtensions { }

    // Helpers
    public static class JsonHelper { }
    public static class HashHelper { }
}
```

### 2. MZikmund.Toolkit.WinUI (existing, enhance)

**Already referenced in both apps - ensure these are included:**

| Component | Description | From App |
|-----------|-------------|----------|
| `IPreferences` | Platform preferences abstraction | Both |
| `IDialogCoordinator` | Dialog queue management | Stopwatch |
| `ILoadingIndicator` | Loading state management | Awaitick |
| `SecurePreferences` | Encrypted preference storage | Both |

**New additions to extract:**

```csharp
// Converters (highly reusable)
namespace MZikmund.Toolkit.WinUI.Converters
{
    public class BoolToVisibilityConverter { }
    public class BoolNegationConverter { }
    public class BoolToObjectConverter { }
    public class NullToVisibilityConverter { }
    public class EnumToLocalizedStringConverter { }
}

// Extensions
namespace MZikmund.Toolkit.WinUI.Extensions
{
    public static class FrameworkElementExtensions { }
    public static class ColorExtensions { }
    public static class DispatcherQueueExtensions { }
}

// Behaviors
namespace MZikmund.Toolkit.WinUI.Behaviors
{
    public class AutoFocusBehavior { }
    public class KeyboardShortcutBehavior { }
}
```

### 3. MZikmund.Toolkit.Uno (new package)

**Uno Platform-specific utilities:**

```csharp
namespace MZikmund.Toolkit.Uno
{
    // Navigation
    public interface INavigationService { }
    public class NavigationService { }
    public class NavigationModel<T> { }

    // Theming
    public interface IThemeManager { }
    public class ThemeManager { }

    // Window management
    public interface IWindowShellProvider { }
    public class WindowShellProvider { }

    // Display
    public interface IDisplayRequestManager { }
    public class DisplayRequestManager { }

    // Localization
    public static class Localizer { }
    public class LocalizeExtension { }
}
```

### 4. MZikmund.Toolkit.Uno.Store (new package)

**In-app purchase abstractions:**

```csharp
namespace MZikmund.Toolkit.Uno.Store
{
    public interface IStoreService { }
    public class WindowsStoreService { }
    public class FakeStoreService { }

    public record ProductInfo(string Id, string Title, string Price);
    public record PurchaseResult(bool Success, string? ErrorMessage);
}
```

### 5. MZikmund.Toolkit.Uno.RevenueCat (new package, optional)

**RevenueCat integration (iOS/Android):**

```csharp
namespace MZikmund.Toolkit.Uno.RevenueCat
{
    public class RevenueCatStoreService : IStoreService { }
    public class RevenueCatConfiguration { }
}
```

### Extraction Priority

| Package | Priority | Effort | Impact |
|---------|----------|--------|--------|
| MZikmund.Toolkit.WinUI (converters) | P0 | Low | High |
| MZikmund.Toolkit.Uno (navigation, theming) | P0 | Medium | Very High |
| MZikmund.Toolkit.Uno.Store | P1 | Medium | High |
| MZikmund.Toolkit.Core (extensions) | P2 | Low | Medium |
| MZikmund.Toolkit.Uno.RevenueCat | P3 | High | Medium |

---

## Implementation Priority

### Phase 1: Core Infrastructure (P0)

1. **Complete WindowShell infrastructure**
   - WindowShellProvider
   - WindowShellViewModel

2. **Theme Management**
   - IThemeManager + ThemeManager
   - Title bar theming

3. **Settings/Preferences**
   - IAppPreferences + AppPreferences

4. **Enhanced Navigation**
   - Convention-based view discovery
   - NavigationModel for parameters

### Phase 2: UI Foundation (P1)

5. **Converter Library**
   - All common converters
   - Converters.xaml resource dictionary

6. **Style Foundation**
   - Colors.xaml with theme support
   - Styles.xaml with button templates
   - Values.xaml with shared dimensions

7. **Dialog Infrastructure**
   - IDialogService + DialogService
   - Confirmation helpers

8. **Localization Enhancements**
   - Localizer static facade
   - LocalizeExtension markup

### Phase 3: Features (P2)

9. **Data Persistence**
   - FileDataService for JSON storage
   - Repository pattern base

10. **Display Management**
    - IDisplayRequestManager

11. **Store Abstraction**
    - IStoreService interface
    - Platform implementations

### Phase 4: Polish (P3)

12. **Onboarding Framework**
    - OnboardingService
    - Template views

13. **System Integration**
    - Sharing service
    - Mail service
    - Rating service

14. **Notifications**
    - Scheduled notification service
    - Platform implementations

---

## File Structure (Target)

```
src/
├── AppTemplate.Core/
│   ├── Services/
│   │   └── INavigationService.cs
│   └── AppTemplate.Core.csproj
│
├── AppTemplate/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── GlobalUsings.cs
│   │
│   ├── Shell/
│   │   ├── IWindowShell.cs
│   │   ├── WindowShell.xaml
│   │   ├── WindowShell.xaml.cs
│   │   ├── WindowShellViewModel.cs
│   │   ├── IWindowShellProvider.cs
│   │   └── WindowShellProvider.cs
│   │
│   ├── Infrastructure/
│   │   ├── IoC.cs
│   │   ├── ViewModelBase.cs
│   │   └── PageViewModel.cs
│   │
│   ├── Services/
│   │   ├── Navigation/
│   │   │   ├── NavigationService.cs
│   │   │   ├── IFrameProvider.cs
│   │   │   ├── FrameProvider.cs
│   │   │   └── NavigationModel.cs
│   │   ├── Theming/
│   │   │   ├── IThemeManager.cs
│   │   │   └── ThemeManager.cs
│   │   ├── Settings/
│   │   │   ├── IAppPreferences.cs
│   │   │   └── AppPreferences.cs
│   │   ├── Dialogs/
│   │   │   ├── IDialogService.cs
│   │   │   ├── DialogService.cs
│   │   │   └── IXamlRootProvider.cs
│   │   ├── Localization/
│   │   │   ├── Localizer.cs
│   │   │   └── LocalizeExtension.cs
│   │   ├── Data/
│   │   │   ├── IDataService.cs
│   │   │   └── FileDataService.cs
│   │   ├── Display/
│   │   │   ├── IDisplayRequestManager.cs
│   │   │   └── DisplayRequestManager.cs
│   │   └── Http/
│   │       └── DebugHttpHandler.cs
│   │
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── BoolNegationConverter.cs
│   │   ├── NullToVisibilityConverter.cs
│   │   ├── EnumLocalizationConverter.cs
│   │   └── ... (others)
│   │
│   ├── Controls/
│   │   ├── PageHeaderControl.xaml
│   │   ├── LoadingOverlay.xaml
│   │   └── EmptyStateControl.xaml
│   │
│   ├── Resources/
│   │   ├── Colors.xaml
│   │   ├── Styles.xaml
│   │   ├── Values.xaml
│   │   ├── Icons.xaml
│   │   ├── Converters.xaml
│   │   └── DataTemplates.xaml
│   │
│   ├── Views/
│   │   ├── MainView.xaml(.cs)
│   │   └── SettingsView.xaml(.cs)
│   │
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Models/
│   │   └── AppConfig.cs
│   │
│   ├── Messages/
│   │   ├── NavigationChangedMessage.cs
│   │   └── ThemeChangedMessage.cs
│   │
│   ├── Strings/
│   │   ├── en/Resources.resw
│   │   └── cs/Resources.resw
│   │
│   ├── Platforms/
│   │   ├── Desktop/Program.cs
│   │   ├── WebAssembly/Program.cs
│   │   ├── iOS/Main.iOS.cs
│   │   └── Android/...
│   │
│   └── AppTemplate.csproj
```

---

## Summary

This specification provides a comprehensive roadmap for enhancing the Uno Platform app template based on proven patterns from two production applications. The key priorities are:

1. **Complete the shell infrastructure** already started on the feature branch
2. **Add theme management** which both apps implement identically
3. **Add preferences/settings** abstraction for persistent app state
4. **Create converter library** for consistent XAML bindings
5. **Extract reusable code to MZikmund.Toolkit** for use across all projects

Following this specification will result in a template that enables rapid development of new Uno Platform applications with production-ready infrastructure from day one.
