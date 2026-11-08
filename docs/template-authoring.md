# Working on the template

This repository is a working app and the source of the template at the same time. There's no separate template
folder: you build, run and debug `src/` as normal, and generation carves optional parts out of it. Everything compiles
with every feature on, so markers live in comments.

The agent-facing rules, including the marker syntax table, are in
[`.claude/rules/template-upgrades.md`](../.claude/rules/template-upgrades.md).

## How the pieces fit

| Path | What it is |
|---|---|
| `.template.config/template.json` | The template: parameters, which files each feature removes, and marker syntax per file type |
| `.template.config/dotnetcli.host.json` | CLI option names, such as `--display-request` |
| `.template.config/ide.host.json` | What the Visual Studio dialog shows |
| `.template.config/wizard.json` | How the wizards group features, and which features require others |
| `template/overlay/` | Files only an app receives: its own `README.md`, `.template-state.json`, the license notice |
| `tools/TemplatePack/` | Packs the repository root as `MZikmund.UnoApp.Templates` and stamps the version into the state file |
| `tools/Wizard.Core/` | Selection rules, manifest, state file and upgrade note parsing. Shared by CLI and UI |
| `tools/Wizard.Cli/` | The `mzuno-app` tool, including the local server for the visual wizard |
| `tools/Wizard.Ui/` | The visual wizard, an Uno WebAssembly app. It has its own `global.json` because it's on Uno.Sdk 7 |
| `docs/upgrades/` | One note per change that apps need to act on |
| `tests/Template.SelfTests/` | Fast tests of the build machinery and `Wizard.Core` |
| `tests/Template.GenerationTests/` | Packs the real template, generates apps, runs the real tool, and compiles the output |

## Adding a feature

Say you're adding a "Telemetry" service that apps can leave out.

1. **Declare it.** In `template.json`, add a parameter under `symbols`. The `replaces` line records the choice in the
   state file:

   ```json
   "telemetry": {
     "type": "parameter",
     "datatype": "bool",
     "displayName": "Telemetry",
     "description": "One sentence about what you get.",
     "defaultValue": "true",
     "replaces": "__opt_telemetry__"
   }
   ```

2. **Register it** in four more places. `RealConfig_*` tests fail if you forget one:
   - `dotnetcli.host.json`: `"telemetry": { "longName": "telemetry", "shortName": "" }`
   - `ide.host.json`: `{ "id": "telemetry", "isVisible": true }`
   - `wizard.json`: add it to a group's `features`, and to `requires` if it needs another feature
   - `template/overlay/.template-state.json`: `"telemetry": "__opt_telemetry__"`

3. **Remove its files when off.** Add to `modifiers` in `template.json`:

   ```json
   { "condition": "(!telemetry)", "exclude": [ "src/AppTemplate/Services/Telemetry/**" ] }
   ```

4. **Mark its lines in shared files**, such as the registration in `App.xaml.cs`, its strings in **both** `.resw` files
   and its package references:

   ```csharp
   //#if (telemetry)
   services.AddSingleton<ITelemetry, Telemetry>();
   //#endif
   ```

5. **Add test rows.** One in `FeatureOff_RemovesItsTraces` and one in `FeatureOff_Builds`. If it has docs, mark the
   links to them so they don't dangle when it's off.

6. **Write an upgrade note** with `features: [telemetry]`.

If your feature needs another one, use a computed symbol, as `useDialogs` does with `(dialogs || rating)`, and test
both directions, as `RatingOn_KeepsDialogsAndLauncherEvenWhenTurnedOff` does.

## Testing

```bash
# Seconds to a couple of minutes: selection rules, state, gate, note parsing
dotnet test tests/Template.SelfTests/Template.SelfTests.csproj

# A few minutes: generated output, leaks, leftover markers, dead links, the tool and upgrade merges
dotnet test tests/Template.GenerationTests/Template.GenerationTests.csproj --filter "TestCategory!=Build"

# Slow: compiles the feature matrix. Needs the android and wasm-tools workloads
dotnet test tests/Template.GenerationTests/Template.GenerationTests.csproj --filter "TestCategory=Build"
```

To try the tool from source without packing the UI:

```bash
dotnet pack tools/TemplatePack/MZikmund.UnoApp.Templates.csproj -o artifacts
dotnet run --project tools/Wizard.Cli/MZikmund.UnoApp.Cli.csproj -- new MyApp --template-source artifacts/MZikmund.UnoApp.Templates.<version>.nupkg
```

To work on the visual wizard, publish it and point the tool at the result:

```bash
cd tools/Wizard.Ui
dotnet publish Wizard.Ui.csproj -c Release -f net10.0-browserwasm -o ../../artifacts/ui
cd ../..
dotnet run --project tools/Wizard.Cli/MZikmund.UnoApp.Cli.csproj -- wizard --ui-root artifacts/ui/wwwroot --template-source artifacts/MZikmund.UnoApp.Templates.<version>.nupkg
```

## Releasing

Both packages share the repository's `version.json`. Run the **Template Package** workflow by hand with `publish`
ticked. It needs a `NUGET_API_KEY` secret. Publish from a `release/v*` branch to get a stable version number.

`mzuno-app upgrade --patch` regenerates an app's old template version from NuGet, so a version an app was created from
must stay published. Apps made from an unpublished build need `--from-source <path to that .nupkg>`.

## Things the engine will do to you

These all came up while building this, and the generation tests guard against each one:

- **Plain `#if DEBUG` gets evaluated and stripped** under the engine's default C# rules, silently dropping the Debug
  branch. `template.json` restricts C# to `//#if` for this reason.
- **NuGet reads an extensionless `PackagePath` as a folder.** `LICENSE` became `LICENSE/LICENSE`, so it stays out of the
  pack and the overlay carries a copy.
- **`sourceName` rewrites `AppTemplate` everywhere**, including inside your own package names and the template's identity.
- **A symbol's `replaces` applies to every file.** Replacing the repository slug globally also rewrote links back to the
  template, which is why `githubDiscussions` replaces only the discussions URL.
- **An MSBuild file above the repository can reach projects outside `src/`,** so `tests/` and `tools/` opt out.
- **NuGet also drops a `.nupkg` nested in a package by default,** which only installing the real tool reveals. The
  **Template Package** workflow does that.
- **Git's global config can break `--patch`.** `diff.noprefix`, `commit.gpgsign` and global hooks all did, so the
  temporary repository ignores it, keeping only the line-ending settings the app itself uses.
- **`git apply` exits 0 and does nothing** for paths outside the current folder, so it runs from the repository top with
  `--directory`.
