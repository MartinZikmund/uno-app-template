# GitHub links in Settings

Settings has a **Feedback & contributing** section with two links: **Report a problem** (`<repo>/issues/new`) and
**Source code** (`<repo>`). It lets people report issues or contribute without hunting for the repository.

## Configuring the URL

One setting, `AppConfig:RepositoryUrl` in `src/AppTemplate/appsettings.json`:

```json
"AppConfig": {
  "RepositoryUrl": "https://github.com/your-org/your-repo"
}
```

`dotnet new` fills it from the **GitHub repository** option (`--github-repo owner/repo`), the same value the issue
template links use. The default is a placeholder, so set the real one.

Empty it (`"RepositoryUrl": ""`), or put in anything that isn't an http(s) address, and the whole section is hidden.
Use that for apps that aren't open source.

## Where it lives

- `AppConfig.RepositoryUrl` holds the setting; `App.RegisterServices` builds a `RepositoryLinks` from it.
- `RepositoryLinks` (Core) validates the URL and builds the two links. `SettingsViewModel.HasRepositoryLinks` drives
  the section's visibility.
- The cards are `ReportProblemLink` and `SourceCodeLink` in `SettingsView.xaml`. Strings are in both `Resources.resw`
  files.
