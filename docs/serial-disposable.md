# Swappable cleanup with SerialDisposable

When a ViewModel tracks the *current* item — and that item owns a resource that must be released the moment the selection moves on — you need to dispose the previous resource before acquiring a new one. `Uno.Disposables.SerialDisposable` makes that hand-off concise and exception-safe.

A typical case: only the selected item should keep the screen awake. `IDisplayRequestManager.RequestActive()` returns an `IDisposable` that holds the request until it is disposed. As the selection changes, the previous request must be released so just one stays active.

## How it works

`SerialDisposable` holds a single inner `IDisposable`. Assigning a new value to its `.Disposable` property automatically disposes whatever it held before. Assigning `null` disposes the current value and holds nothing. This removes the need for a manual null check and a separate `Dispose()` call before every reassignment.

## Example

`IDisplayRequestManager` is a head-only service (`AppTemplate.Services`), so a ViewModel that depends on it belongs in the head namespace (`AppTemplate.ViewModels`), not `AppTemplate.Core.ViewModels` — `AppTemplate.Core` can't reference head-only types:

```csharp
using AppTemplate.Core.ViewModels;
using AppTemplate.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Uno.Disposables;

namespace AppTemplate.ViewModels;

public partial class ItemListViewModel : ViewModelBase
{
    private readonly IDisplayRequestManager _displayRequestManager;
    private readonly SerialDisposable _displayRequestDisposable = new();

    public ItemListViewModel(IDisplayRequestManager displayRequestManager)
    {
        _displayRequestManager = displayRequestManager;
    }

    // ItemModel stands in for whatever type your list actually holds.
    [ObservableProperty]
    public partial ItemModel? SelectedItem { get; set; }

    partial void OnSelectedItemChanged(ItemModel? value)
    {
        // Assigning here releases the previous item's display request before
        // acquiring one for the new selection. Assigning null releases it entirely.
        _displayRequestDisposable.Disposable = value is { KeepScreenOn: true }
            ? _displayRequestManager.RequestActive()
            : null;
    }

    public override void OnNavigatedFrom()
    {
        // ViewModels are resolved from a per-window scope and aren't disposed just
        // because the page unloads — clear the field explicitly to release now.
        _displayRequestDisposable.Disposable = null;
    }
}
```

The same idiom wraps any custom cleanup that should run on the next swap — not just an `IDisposable` handed to you by a service. Pass a callback to `Disposable.Create(...)`. For example, unhooking an event handler that was attached for the current selection (capture `value` into a local so the closure unsubscribes the right instance):

```csharp
partial void OnSelectedItemChanged(ItemModel? value)
{
    var item = value;
    item?.SomethingChanged += OnSomethingChanged;

    _displayRequestDisposable.Disposable = item is not null
        ? Disposable.Create(() => item.SomethingChanged -= OnSomethingChanged)
        : null;
}
```

## When to prefer `SerialDisposable` over manual dispose-then-reassign

| Concern | Manual pattern | `SerialDisposable` |
|---|---|---|
| Null-safety | Requires an explicit null check before calling `.Dispose()` | Handles `null` automatically |
| Consistency | Easy to forget the `Dispose()` call, or get the order wrong, at some call site | A single assignment always disposes the previous value — nothing to forget |
| Readability | Two statements for every "swap" | One statement |

## Teardown

ViewModels in this template are resolved from a per-window DI scope and aren't disposed when you navigate away from a page — only when the window itself closes. Don't rely on `Dispose()` running just because a page unloads; clear the field explicitly instead, as `OnNavigatedFrom()` does above. That's the common case.

Only implement `IDisposable` on the ViewModel and call `_displayRequestDisposable.Dispose()` if you genuinely control its end of life. Once a `SerialDisposable` has been disposed, assigning `.Disposable` again disposes the new value immediately instead of holding it — so don't call `Dispose()` from a hook that might fire more than once.
