---
title: Add Feedback & contributing links to Settings
impact: optional
features: []
platforms: []
---

## Why

Settings gains a **Feedback & contributing** section with "Report a problem" (`<repo>/issues/new`) and "Source code"
(`<repo>`) links, so people can reach your GitHub repository. It hides itself when no repository URL is set.

## Who is affected

Apps that want those links. Skip this if your app is closed source.

## Steps

1. Add `RepositoryUrl` to `AppConfig` in `src/AppTemplate/Models/AppConfig.cs` and to `appsettings.json`:

   ```csharp
   public string? RepositoryUrl { get; init; }
   ```

   ```json
   "AppConfig": { "Environment": "Production", "RepositoryUrl": "https://github.com/your-org/your-repo" }
   ```

2. Copy `src/AppTemplate.Core/Models/RepositoryLinks.cs` from the template into your app's Core project.
3. In `App.RegisterServices`, add
   `services.AddSingleton(sp => new RepositoryLinks(sp.GetRequiredService<IOptions<AppConfig>>().Value.RepositoryUrl));`.
4. In `SettingsViewModel`, take a `RepositoryLinks` constructor parameter and expose it as `RepositoryLinks` plus
   `public bool HasRepositoryLinks => RepositoryLinks.IsAvailable;`.
5. Copy the "Feedback section" block from `SettingsView.xaml`, the three click handlers from `SettingsView.xaml.cs`,
   and the five new strings (`FeedbackAndContributing`, `ReportProblem`, `ReportProblemDescription`, `SourceCode`,
   `SourceCodeDescription`) into every `Resources.resw` you ship.

## Verify

Build, open Settings, and check that both links open your repository. Set `RepositoryUrl` to `""` and the section
disappears.
