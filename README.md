# template

## Views

### `IViewBase`

`ViewBase<TViewModel>` is the generic base class for views (pages). It resolves the view
model from the hosting `WindowShell` service provider and forwards lifecycle events to it.

`IViewBase` is a non-generic interface that exposes the type-erased surface of
`ViewBase<TViewModel>` — currently the resolved view model as `object?`:

```csharp
public interface IViewBase
{
    object? ViewModel { get; }
}
```

Every `ViewBase<TViewModel>` implements `IViewBase`, so you can reference a view through the
interface without taking a dependency on the concrete generic type.

Use it when:

- A `DataTemplate` (or other shared/loosely-typed code) needs to call into a view without
  knowing — or depending on — the specific view model generic argument.
- A test needs to inspect a view's resolved view model without knowing its concrete type at
  compile time.
