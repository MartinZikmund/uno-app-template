# WinUI TitleBar control integration — design

**Date:** 2026-06-01
**Branch:** `features/titlebar`
**Status:** Approved for planning

## Goal

Replace the template's hand-rolled custom title bar (a `Grid` placeholder plus a
separate app-icon `Grid`, gated by `#if !HAS_UNO` and `HasCustomTitleBar`) with the
real WinUI `TitleBar` control (`Microsoft.UI.Xaml.Controls.TitleBar`), including a
search box in its content area.

The control is **enabled on the Windows (WinUI) head only**. On the four Uno heads
(`net10.0-desktop`, `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`) the
control is omitted entirely and the `NavigationView` keeps its own back / pane-toggle
chrome plus the system title bar, exactly as today. Uno **Desktop** support is a future
step — the design is structured so enabling it later is a condition change, not a
rewrite.

## Background / why

`Microsoft.UI.Xaml.Controls.TitleBar` is not implemented by Uno Platform yet (Uno only
exposes the lower-level `AppWindowTitleBar.ExtendsContentIntoTitleBar` /
`Window.SetTitleBar` window APIs). Referencing the `TitleBar` type from shared XAML or
shared C# therefore breaks the Uno builds. The integration must keep all `TitleBar`
references on the Windows head.

Reference implementation followed for layout, wiring, and UX:
`D:\Personal\WinUI-Gallery\WinUIGallery\MainWindow.xaml` and `MainWindow.xaml.cs`.

## Decisions (from brainstorming)

1. **Cross-target mechanism:** Uno **conditional XAML** (`win:` prefix) in the shared
   `WindowShell.xaml`, with Windows-only code-behind under `#if !HAS_UNO`.
   (Chosen over building the control in C#, or extracting a separate UserControl.)
2. **Search:** a **plain, non-interactive** `AutoSuggestBox` placed in
   `TitleBar.Content`. No search service, no view-model members, no event handlers, no
   bindings — it renders as a correctly-sized placeholder and is the extension point an
   app wires up later. No search boilerplate ships in the template.
3. **Back + pane-toggle ownership:** the `TitleBar` owns the back button and pane-toggle
   on Windows; the `NavigationView`'s own back/toggle are hidden on Windows and retained
   on every other head.
4. **Title text:** `TitleBar.Title` = application name (static, localized);
   `TitleBar.Subtitle` = current page/section title (the existing `ViewModel.Title`).
5. **NavigationView margin workaround (WinUI #9934):** **not** included initially. Only
   add it if the caption-button / NavigationView gap is actually observed when running
   the Windows app during verification.

## Scope

### In scope
- `WindowShell.xaml`: introduce the `win:TitleBar`; remove the old `TitleBarGrid`, the
  app-icon `Grid`, and `HasCustomTitleBar`; add `win:` / `not_win:` conditionals to the
  `NavigationView`.
- `WindowShell.xaml.cs`: Windows-only window setup + `BackRequested` / `PaneToggleRequested`
  handlers; remove `HasCustomTitleBar`; repoint the `SetTitleBar` fallback.
- One new localization key (`SearchPlaceholder`) in `en` and `cs`.

### Out of scope
- Any working search behavior (no `ISearchService`, no VM changes, no DI changes).
- Enabling the control on Uno Desktop (future step).
- The `DevChannelBadge` overlay (left untouched).
- The `#9934` margin workaround (conditional on observed need).

## Detailed design

### 1. Conditional XAML prefixes

At the top of `WindowShell.xaml`, declare the Uno conditional prefixes:

- `win` → namespace `http://schemas.microsoft.com/winfx/2006/xaml/presentation`,
  **not** listed in `mc:Ignorable` (included on Windows, stripped on Uno heads).
- `not_win` → an arbitrary namespace, **listed** in `mc:Ignorable` (stripped on Windows,
  included on Uno heads).
- `mc` (`http://schemas.openxmlformats.org/markup-compatibility/2006`) for `mc:Ignorable`.

### 2. `WindowShell.xaml`

Row 0 (`Height="Auto"`) hosts the title bar; on Uno heads the `win:`-prefixed element is
stripped and the row collapses to zero height.

```xml
<win:TitleBar
    x:Name="AppTitleBar"
    Title="{markup:Localize Key=ApplicationName}"
    Subtitle="{x:Bind ViewModel.Title, Mode=OneWay}"
    IsBackButtonVisible="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"
    IsPaneToggleButtonVisible="True"
    BackRequested="AppTitleBar_BackRequested"
    PaneToggleRequested="AppTitleBar_PaneToggleRequested">
    <win:TitleBar.Resources>
        <!-- Content defaults to Center; Stretch lets the search box fill to MaxWidth. -->
        <HorizontalAlignment x:Key="TitleBarContentHorizontalAlignment">Stretch</HorizontalAlignment>
    </win:TitleBar.Resources>
    <win:TitleBar.IconSource>
        <ImageIconSource ImageSource="ms-appx:///Assets/Icons/icon_foreground.png" />
    </win:TitleBar.IconSource>
    <win:TitleBar.Content>
        <!-- Plain, non-interactive placeholder. Apps wire up search here. -->
        <AutoSuggestBox
            MaxWidth="580"
            HorizontalAlignment="Stretch"
            VerticalAlignment="Center"
            PlaceholderText="{markup:Localize Key=SearchPlaceholder}"
            QueryIcon="Find" />
    </win:TitleBar.Content>
</win:TitleBar>
```

Notes:
- Nested property-element tags use the `win:` prefix (`win:TitleBar.IconSource`,
  `win:TitleBar.Content`, `win:TitleBar.Resources`) so the whole subtree is stripped
  together on Uno heads.
- `markup:Localize` and `x:Bind` only ever execute on the Windows head, so referencing
  `ApplicationName` / `ViewModel.Title` / `ViewModel.CanGoBack` here is safe.
- If `markup:Localize` cannot be applied to `TitleBar.Title` for any reason, fall back to
  setting `AppTitleBar.Title` from the localizer in the Windows-only code-behind.

`NavigationView` (Row 1) — hide its own back/toggle on Windows, keep them everywhere else:

```xml
<NavigationView
    x:Name="NavView"
    Grid.Row="1"
    win:IsBackButtonVisible="Collapsed"
    not_win:IsBackButtonVisible="Auto"
    win:IsPaneToggleButtonVisible="False"
    not_win:IsPaneToggleButtonVisible="True"
    IsBackEnabled="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"
    ... existing attributes unchanged ... >
```

`IsBackEnabled` stays unprefixed (harmless on Windows where the button is collapsed).
All other `NavigationView` attributes, menu items, the `InnerFrame`, and the
`DevChannelBadge` overlay are unchanged.

Removed from the XAML: the `TitleBarGrid` element, the app-icon `Grid`, and both
`Visibility="{x:Bind HasCustomTitleBar}"` bindings.

### 3. `WindowShell.xaml.cs`

`CustomizeWindow()` (already `#if !HAS_UNO`) becomes:

```csharp
#if !HAS_UNO
    if (AppWindowTitleBar.IsCustomizationSupported())
    {
        _associatedWindow.ExtendsContentIntoTitleBar = true;
        _associatedWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        _associatedWindow.SetTitleBar(AppTitleBar);
    }
#endif
    // Mica backdrop block unchanged.
```

Windows-only event handlers (under `#if !HAS_UNO`):

```csharp
private void AppTitleBar_BackRequested(TitleBar sender, object args)
{
    var nav = ServiceProvider.GetRequiredService<INavigationService>();
    if (nav.GoBack())
    {
        UpdateNavigationViewSelection();
    }
}

private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    => NavView.IsPaneOpen = !NavView.IsPaneOpen;
```

Other changes:
- Remove the `public bool HasCustomTitleBar { get; private set; }` property.
- `SetTitleBar(UIElement?)` fallback changes from `TitleBarGrid` to `AppTitleBar`. Because
  `AppTitleBar` only exists on the Windows head, the body that references it must be
  `#if !HAS_UNO`; the non-Windows path keeps today's behavior (pass the element straight
  to `_associatedWindow.SetTitleBar`, or no-op when null).

Keep the existing `using Windows.Foundation.Metadata;` / `Microsoft.UI.Windowing` usings;
add `Microsoft.UI.Xaml.Controls` if the `TitleBar` type is not already in scope (also
inside the guard if needed).

### 4. Localization

Add to `src/AppTemplate/Strings/en/Resources.resw` and
`src/AppTemplate/Strings/cs/Resources.resw`:

- `SearchPlaceholder` — en: `Search…`  cs: `Hledat…`

### 5. Tests

No new business logic is introduced (the `AutoSuggestBox` is unwired and
`WindowShellViewModel` is unchanged), so there are **no new unit tests**. Verification is
done by building and running, not by MSTest.

## Cross-target behavior summary

| Head | Title bar |
| --- | --- |
| `net10.0-windows10.0.26100` | WinUI `TitleBar` control: icon, title, page subtitle, back + pane-toggle, placeholder search box. |
| `net10.0-desktop` (Uno) | No `TitleBar` control. `NavigationView` keeps its own back/toggle; system title bar. (Future: enable the control here.) |
| `net10.0-android` / `net10.0-ios` / `net10.0-browserwasm` | No `TitleBar` control; `NavigationView` chrome as today. |

## Verification

1. Build every target head and confirm all compile — the four Uno heads must strip the
   `win:TitleBar` cleanly (no `TitleBar`-type resolution errors):
   `net10.0-windows10.0.26100`, `net10.0-desktop`, `net10.0-android`, `net10.0-ios`,
   `net10.0-browserwasm`.
2. Run the **Windows** app and confirm:
   - The `TitleBar` shows the app icon, app name (Title), and current page (Subtitle).
   - The pane-toggle opens/closes the `NavigationView` pane.
   - The back button appears only when `CanGoBack` is true and navigates back.
   - The search `AutoSuggestBox` renders at the right size with placeholder + find icon
     (typing does nothing, by design).
   - No double back/pane-toggle (NavigationView's own are hidden on Windows).
3. If a gap/misalignment appears between the caption buttons and the NavigationView (and
   shifts on maximize/restore), add the WinUI #9934 margin workaround; otherwise leave it
   out.
4. (Optional) Run the Uno Desktop head and confirm the system title bar + NavigationView
   chrome behave exactly as before this change.

## Risks / open items

- **Conditional-XAML quirks:** nested `win:`-prefixed property elements and `win:` /
  `not_win:` property conditionals must parse correctly across heads. Mitigation: build
  all heads early (verification step 1).
- **`markup:Localize` on `TitleBar.Title`:** if unsupported, set the title from the
  localizer in code-behind (Windows-only). Low risk.
- **WinAppSDK version:** the `TitleBar` control requires WinAppSDK ≥ 1.6; Uno.Sdk
  `6.7.0-dev.64` resolves a newer WinAppSDK, so it is expected to be available. Confirm at
  first Windows build.
```
