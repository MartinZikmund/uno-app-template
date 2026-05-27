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

The repository ships a local tool manifest (`.config/dotnet-tools.json`). Restore the tools once after cloning:

```bash
dotnet tool restore
```

### Build and Run

```bash
cd src/AppTemplate
dotnet build -f net10.0-windows10.0.26100
dotnet run -f net10.0-windows10.0.26100
```

Swap the target framework (`net10.0-android`, `net10.0-ios`, `net10.0-desktop`, `net10.0-browserwasm`) to build for other platforms.

## XAML Styler

XAML formatting is kept consistent with [XAML Styler](https://github.com/Xavalon/XamlStyler), pinned as a local dotnet tool. The [`Settings.XamlStyler`](Settings.XamlStyler) file at the repository root is picked up automatically.

Format all XAML files under `src/`:

```bash
dotnet xstyler --recursive --directory src
```

Or verify formatting without writing changes (useful in CI):

```bash
dotnet xstyler --recursive --directory src --passive
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
