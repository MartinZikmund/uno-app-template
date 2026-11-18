# Cross-head gotchas

You're about to ship something that works on Windows and quietly misbehaves on Android, WebAssembly or
Skia. These are the traps Daily Plants hit first, so you don't have to. Snippets marked *illustrative*
use placeholder names that don't exist in this repo.

## Use `Tapped`, not `PointerPressed`, for tap actions in scrollable lists

On touch, `PointerPressed` fires before the platform knows whether the finger is tapping or starting a
scroll, so scrolling a list triggers the row action. `Tapped` is raised only after a discrete tap is
recognised. Source: [daily-plants#11](https://github.com/MartinZikmund/daily-plants/pull/11).

```xml
<!-- illustrative -->
<Grid Tapped="ItemCard_Tapped" />
```

## `ItemsRepeater` doesn't set `DataContext` on `x:Bind` templates

Inside an `ItemsRepeater` template that uses `x:Bind`, `DataContext` is not set on the root element.
A handler that does `(sender as FrameworkElement)?.DataContext as Item` gets `null` and silently does
nothing. Pass the item on `Tag` (`Tag="{x:Bind}"`) or bind a command instead. Source:
[daily-plants#61](https://github.com/MartinZikmund/daily-plants/pull/61).

## `UniformGridLayout.MinItemHeight` is the exact item height

Despite the name, it is not a minimum. Set it to 4 and every item is 4px tall, clipping anything taller
inside. Leave it unset and let the item template decide the height. The same goes for `MinItemWidth` if
you rely on content sizing. Source: [daily-plants#80](https://github.com/MartinZikmund/daily-plants/pull/80).

## `WindowActivationState` is a different type per head

On WinAppSDK it is `Microsoft.UI.Xaml.WindowActivationState`; on Uno heads it is
`Windows.UI.Core.CoreWindowActivationState`. No single comparison compiles on both, and `#if` blocks
in C# are a maintenance tax. Don't read the state: make the activation handler idempotent and run it on
every activation. Source: [daily-plants#69](https://github.com/MartinZikmund/daily-plants/pull/69).

```csharp
// illustrative: cheap no-op when nothing changed, so no state check needed
window.Activated += async (_, _) => await viewModel.RefreshIfDateChangedAsync();
```

## Don't cache "today"

A page that captures `DateTime.Today` once keeps it forever. Leave the app open past midnight and it
writes to yesterday. Inject `TimeProvider` (so tests can fake the clock), read it when you need it, and
re-check on activation. Source: [daily-plants#68](https://github.com/MartinZikmund/daily-plants/pull/68).

```csharp
// illustrative
public sealed class DiaryViewModel(TimeProvider time)
{
    private DateOnly _today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void RefreshIfDateChanged()
    {
        DateOnly now = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        if (now != _today)
        {
            _today = now;
            // reload
        }
    }
}
```

## Rebuild localized strings cached in services when the language changes

Anything that resolves strings once and keeps them (a checklist, a list of labels) shows the old
language after a switch. Rebuild it on the language-changed event. Also watch startup order: if the
cache warms before the saved language is applied, it holds default-language strings until the next
change. Apply the saved language first, then build caches. Sources:
[daily-plants#69](https://github.com/MartinZikmund/daily-plants/pull/69),
[daily-plants#71](https://github.com/MartinZikmund/daily-plants/pull/71).

## Opening a settings page must not write preferences

Building toggles from saved state can fire their `Toggled`/`Checked` handlers, which then write the
same values back as if the user had changed them. Guard the handlers (or the setters) while the page
loads, and only persist when the value really differs. Source:
[daily-plants#71](https://github.com/MartinZikmund/daily-plants/pull/71).

## WebAssembly can't fetch third-party feeds without CORS

Browsers block cross-origin requests to servers that don't send `Access-Control-Allow-Origin`, and
there is nothing the app can do about it. On the WASM head, serve from the cache and tell the user
that live content isn't available, instead of showing a spinner forever. Source:
[daily-plants#64](https://github.com/MartinZikmund/daily-plants/pull/64).

## Some Android failures only show in Release

Debug builds on Android add the `INTERNET` permission for you (for the debugger), so a manifest that
forgot it works fine until a Release build can't reach the network. Test network features in Release
too. Declaring the permission in the template's manifest is tracked in
[#102](https://github.com/MartinZikmund/uno-app-template/issues/102) /
[#115](https://github.com/MartinZikmund/uno-app-template/pull/115); until that lands, add it yourself.
