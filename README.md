# App Template

## Views

### `IViewBase`

`ViewBase<TViewModel>` is the base class for views (pages). It resolves the view model from the
hosting `WindowShell` service provider and forwards lifecycle events to it.

`IViewBase` is a non-generic interface that exposes the resolved view model as `object?`:

```csharp
public interface IViewBase
{
    object? ViewModel { get; }
}
```

Every `ViewBase<TViewModel>` implements `IViewBase`, so a view can be referenced through the
interface without depending on its concrete view model type.

Use it when:

- A `DataTemplate` (or other loosely-typed code) needs to reach a view's view model without
  knowing its generic argument.
- A test needs to inspect a view's resolved view model without knowing its concrete type at
  compile time.

### Restore dotnet tools

```bash
dotnet tool restore
```

## XAML Styler

This repo includes a [`Settings.XamlStyler`](Settings.XamlStyler) config at the root and uses [XAML Styler](https://github.com/Xavalon/XamlStyler) to enforce consistent XAML formatting.

### Running XAML Styler

First restore the local dotnet tool:

```bash
dotnet tool restore
```

Then format all XAML files under `src/`:

```bash
dotnet xstyler --recursive --directory src
```

Or to check formatting without making changes (useful in CI):

```bash
dotnet xstyler --recursive --directory src --passive
```

The `Settings.XamlStyler` file at the repo root is automatically picked up by the tool.
