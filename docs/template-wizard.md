# Creating an app with the wizard

You want a new Uno app with only the parts you need. There are four ways in, and they all produce the same output
because they all run the same template.

## In your browser

```bash
dotnet tool install -g MZikmund.UnoApp.Cli
mzuno-app wizard
```

This opens a local page with a step for each group of choices and a live preview of the equivalent command. **Create
app** writes the app to disk. It's a local server bound to loopback, reachable only from the browser tab it opened,
and it stops when you press Ctrl+C.

### Pull request previews

Each pull request on the template repository gets a hosted copy of the wizard at a stable address, posted as a comment
by [`template-wizard-preview.yml`](../.github/workflows/template-wizard-preview.yml). It sits next to the app's own
preview and uses its own Cloudflare Worker, `uno-app-wizard`.

The preview is read-only. It has no server, so `mzuno-app write-preview` adds `api/manifest.json` and `api/info.json` to
the published UI as plain files, built from the same config the tool reads. The info file sets `canCreate` to false,
which hides **Create app**, the output folder and the git option. Every step and the live command still work.

Nothing is served at the Worker's own address, only at each preview's alias. The first run creates the Worker with a
deploy, and later runs only upload versions. This needs no extra secrets beyond the app preview's
`CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID`. Apps created from the template get none of this.

## In your terminal

```bash
mzuno-app new              # asks for name, identity, platforms and features
```

Anything you pass isn't asked again, and `--yes` skips the questions entirely, which suits scripts and agents:

```bash
mzuno-app new Contoso --yes --platforms windows,desktop --without rating,spec-kit --app-id com.contoso.app
```

Unless you pass `--no-git`, the tool also creates a git repository with an initial commit. `mzuno-app features` lists
everything you can pick, and `--dry-run` prints the `dotnet new` command instead of running it.

## With plain `dotnet new`

```bash
dotnet new install MZikmund.UnoApp.Templates
dotnet new mzuno-app -n Contoso --platforms windows --platforms desktop --rating false
```

Every feature is on by default, so you turn off what you don't want. `dotnet new mzuno-app --help` lists the options.
This route doesn't create a git repository, though the app builds without one. It also doesn't check your values the
way the wizards do: a `&` or a quote in `--title` or `--publisher` produces files that won't parse.

## In Visual Studio or Rider

After `dotnet new install`, the template appears in **Create a new project**. Search for "Uno App" or filter by the
"Uno Platform" tag. The options show up as a form on the "Additional information" page. It's a flat list, so it doesn't
explain dependencies. Switching off **Dialog services** while **App rating prompt** is on just keeps the dialogs,
because rating needs them.

## What depends on what

| Feature | Also includes |
|---|---|
| App rating prompt | Dialog services, Launcher service |
| Spec Kit | Coding agent guidance |

The wizards show this as you pick. Plain `dotnet new` applies the same rule without comment.

## After creating

Your app's `README.md` lists what to do before a first release. `.template-state.json` records your choices: keep it
committed, because [upgrading later](./template-upgrades.md) depends on it.
