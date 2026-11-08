# Uno App template

A production-shaped starting point for a cross-platform [Uno Platform](https://platform.uno/) app.

```bash
dotnet new install MZikmund.UnoApp.Templates
dotnet new mzuno-app -n MyApp --platforms windows --platforms desktop --rating false
```

Prefer to pick features interactively? Install the wizard:

```bash
dotnet tool install -g MZikmund.UnoApp.Cli
mzuno-app new        # guided, in the terminal
mzuno-app wizard     # visual, in your browser
```

Documentation: <https://github.com/MartinZikmund/uno-app-template>
