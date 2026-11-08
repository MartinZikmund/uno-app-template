using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;

namespace Template.GenerationTests;

/// <summary>Runs the real tool against a real app, with a second template version made by editing the real package.</summary>
[TestClass]
public class UpgradeTests
{
    private static readonly string _tool = Path.Combine(AppContext.BaseDirectory, "mzuno-app.dll");

    private static string Tool(string workingDirectory, params string[] arguments) =>
        TemplateFixture.Run("dotnet", [_tool, .. arguments], workingDirectory);

    private static string ToolWithEnvironment(string workingDirectory, Dictionary<string, string> environment, params string[] arguments) =>
        TemplateFixture.Run("dotnet", [_tool, .. arguments], workingDirectory, environment);

    private static GeneratedApp CreateApp(string scenario, params string[] options)
    {
        var path = Path.Combine(TemplateFixture.WorkRoot, scenario);
        Tool(TemplateFixture.WorkRoot, ["new", "ContosoNotes", "-o", path, "--yes", "--template-source", TemplateFixture.PackagePath, .. options]);
        return new GeneratedApp(path, "ContosoNotes");
    }

    private static void Commit(GeneratedApp app, string message)
    {
        TemplateFixture.Run("git", ["add", "-A"], app.Root);
        TemplateFixture.Run("git", ["-c", "user.name=T", "-c", "user.email=t@example.com", "commit", "-q", "-m", message], app.Root);
    }

    /// <summary>A "next version" of the template: the same package with some files changed.</summary>
    private static string NextVersion(string scenario, Dictionary<string, Func<string, string?>> edits, Dictionary<string, string>? additions = null)
    {
        var path = Path.Combine(TemplateFixture.WorkRoot, $"{scenario}-next.nupkg");
        File.Copy(TemplateFixture.PackagePath, path, overwrite: true);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        foreach (var (entryPath, edit) in edits)
        {
            var name = $"content/UnoApp/{entryPath}";
            var entry = archive.GetEntry(name) ?? throw new FileNotFoundException(name);
            string text;
            using (StreamReader reader = new(entry.Open()))
            {
                text = reader.ReadToEnd();
            }

            entry.Delete();
            if (edit(text) is { } edited)
            {
                using StreamWriter writer = new(archive.CreateEntry(name).Open());
                writer.Write(edited);
            }
        }

        foreach (var (entryPath, text) in additions ?? [])
        {
            using StreamWriter writer = new(archive.CreateEntry($"content/UnoApp/{entryPath}").Open());
            writer.Write(text);
        }

        return path;
    }

    [TestMethod]
    public void New_CreatesARepositoryWithACleanStart()
    {
        var app = CreateApp("tool-new", "--without", "rating", "--platforms", "desktop");

        app.Exists(".git").Should().BeTrue();
        app.Exists("src/ContosoNotes/Services/Rating").Should().BeFalse();
        TemplateFixture.Run("git", ["log", "--oneline"], app.Root).Should().Contain("create app");
        TemplateFixture.Run("git", ["status", "--porcelain"], app.Root).Trim().Should().BeEmpty();
    }

    [TestMethod]
    public void New_RefusesANonEmptyFolder()
    {
        var path = Path.Combine(TemplateFixture.WorkRoot, "tool-occupied");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "precious.txt"), "keep me");

        var act = () => Tool(TemplateFixture.WorkRoot, "new", "ContosoNotes", "-o", path, "--yes", "--template-source", TemplateFixture.PackagePath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*isn't empty*");
        File.ReadAllText(Path.Combine(path, "precious.txt")).Should().Be("keep me");
    }

    [TestMethod]
    public void Upgrade_FreshApp_HasNothingPending()
    {
        var app = CreateApp("tool-fresh");

        Tool(app.Root, "upgrade").Should().Contain("Nothing pending");
    }

    [TestMethod]
    public void Upgrade_ListsOnlyWhatApplies_AndRecordsProgress()
    {
        var app = CreateApp("tool-pending", "--without", "worktree-identity");
        var statePath = app.Path(".template-state.json");

        // Pretend the app predates every note.
        var state = File.ReadAllText(statePath);
        using (var document = JsonDocument.Parse(state))
        {
            var applied = document.RootElement.GetProperty("appliedUpgrades").EnumerateArray().Select(e => $"\"{e.GetString()}\"");
            File.WriteAllText(statePath, state.Replace(string.Join(", ", applied), ""));
        }

        var list = Tool(app.Root, "upgrade");
        list.Should().Contain("2026-11-08-template-state-file");
        list.Should().NotContain("2026-09-06-worktree-identity", "this app turned that feature off");

        Tool(app.Root, "upgrade", "--show", "2026-11-08-template-state-file").Should().Contain("## Steps");

        Tool(app.Root, "upgrade", "--mark-applied", "2026-11-08-template-state-file");
        Tool(app.Root, "upgrade").Should().NotContain("2026-11-08-template-state-file");

        var json = Tool(app.Root, "upgrade", "--json");
        using var pending = JsonDocument.Parse(json);
        pending.RootElement.EnumerateArray().Select(n => n.GetProperty("id").GetString()).Should().Contain("2026-05-28-versioning-model");
    }

    [TestMethod]
    public void Upgrade_MarkApplied_RejectsUnknownNote()
    {
        var app = CreateApp("tool-unknown");

        var act = () => Tool(app.Root, "upgrade", "--mark-applied", "2099-01-01-nope");

        act.Should().Throw<InvalidOperationException>().WithMessage("*2099-01-01-nope*");
    }

    [TestMethod]
    public void Patch_MergesTemplateChangesAroundTheAppsOwnEdits()
    {
        var app = CreateApp("tool-merge", "--platforms", "desktop", "--title", "Contoso Notes", "--app-id", "com.contoso.notes", "--without", "rating");

        // The app's own change, far from what the template will touch.
        var code = app.Path("src/ContosoNotes/App.xaml.cs");
        File.WriteAllText(code, File.ReadAllText(code).Replace("public App()", "// The app's own comment\n    public App()"));
        Commit(app, "app change");

        var next = NextVersion(
            "merge",
            new()
            {
                ["src/AppTemplate/App.xaml.cs"] = text => text.Replace("private static void ConfigureLogging", "// From the new template\n    private static void ConfigureLogging"),
            },
            new() { ["src/AppTemplate/Services/NewFromTemplate.cs"] = "namespace AppTemplate.Services;\n\npublic sealed class NewFromTemplate;\n" });

        var output = Tool(app.Root, "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", next);

        output.Should().Contain("cleanly");
        var merged = File.ReadAllText(code);
        merged.Should().Contain("The app's own comment").And.Contain("From the new template");
        app.Exists("src/ContosoNotes/Services/NewFromTemplate.cs").Should().BeTrue("new files arrive under the app's own name");
        app.Read("src/ContosoNotes/Services/NewFromTemplate.cs").Should().Contain("namespace ContosoNotes.Services");
        app.Read("src/ContosoNotes/ContosoNotes.csproj").Should().Contain("com.contoso.notes", "the app's identity must survive");
        app.Exists("src/ContosoNotes/Services/Rating").Should().BeFalse("a feature the app dropped must not come back");
    }

    [TestMethod]
    public void Patch_ConflictingEdit_LeavesMarkersAndNamesTheFile()
    {
        var app = CreateApp("tool-conflict", "--platforms", "desktop");
        var mainView = app.Path("src/ContosoNotes/Views/MainView.xaml");
        File.WriteAllText(mainView, File.ReadAllText(mainView).Replace("Hello Uno Platform!", "Hello from the app"));
        Commit(app, "app change");

        var next = NextVersion("conflict", new()
        {
            ["src/AppTemplate/Views/MainView.xaml"] = text => text.Replace("Hello Uno Platform!", "Hello from the template"),
        });

        var output = Tool(app.Root, "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", next);

        output.Should().Contain("conflict").And.Contain("MainView.xaml");
        File.ReadAllText(mainView).Should().Contain("<<<<<<<").And.Contain("Hello from the app").And.Contain("Hello from the template");
    }

    private static readonly Dictionary<string, Func<string, string?>> _appCodeEdit = new()
    {
        ["src/AppTemplate/App.xaml.cs"] = text => text.Replace("private static void ConfigureLogging", "// From the new template\n    private static void ConfigureLogging"),
    };

    private static string Patch(GeneratedApp app, string next) =>
        Tool(app.Root, "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", next);

    [TestMethod]
    public void Patch_AppInASubfolderOfItsRepository_ReallyChangesFiles()
    {
        // git apply exits 0 and silently skips paths outside the current folder.
        var repository = Path.Combine(TemplateFixture.WorkRoot, "tool-mono");
        var path = Path.Combine(repository, "apps", "ContosoNotes");
        Tool(TemplateFixture.WorkRoot, "new", "ContosoNotes", "-o", path, "--yes", "--no-git", "--platforms", "desktop", "--template-source", TemplateFixture.PackagePath);
        TemplateFixture.Run("git", ["init", "-q"], repository);
        TemplateFixture.Run("git", ["add", "-A"], repository);
        TemplateFixture.Run("git", ["commit", "-q", "-m", "monorepo"], repository);
        GeneratedApp app = new(path, "ContosoNotes");

        Patch(app, NextVersion("mono", _appCodeEdit));

        app.Read("src/ContosoNotes/App.xaml.cs").Should().Contain("From the new template");
    }

    [TestMethod]
    public void Patch_AppDeletedAFileTheTemplateChanged_SkipsItAndMergesTheRest()
    {
        var app = CreateApp("tool-deleted", "--platforms", "desktop");
        File.Delete(app.Path("src/ContosoNotes/Views/MainView.xaml"));
        Commit(app, "remove the sample page");
        Dictionary<string, Func<string, string?>> edits = new(_appCodeEdit)
        {
            ["src/AppTemplate/Views/MainView.xaml"] = text => text.Replace("Hello Uno Platform!", "Hello from the template"),
        };

        var output = Patch(app, NextVersion("deleted", edits));

        app.Read("src/ContosoNotes/App.xaml.cs").Should().Contain("From the new template");
        app.Exists("src/ContosoNotes/Views/MainView.xaml").Should().BeFalse("the app removed it on purpose");
        output.Should().Contain("MainView.xaml").And.Contain("Skipped");
    }

    [TestMethod]
    public void Patch_AppEditedAFileTheTemplateDeleted_KeepsItAndMergesTheRest()
    {
        var app = CreateApp("tool-kept", "--platforms", "desktop");
        var share = app.Path("src/ContosoNotes/Services/ShareService.cs");
        File.WriteAllText(share, File.ReadAllText(share) + "\n// the app's own change\n");
        Commit(app, "customize share");
        Dictionary<string, Func<string, string?>> edits = new(_appCodeEdit)
        {
            ["src/AppTemplate/Services/ShareService.cs"] = _ => null,
        };

        var output = Patch(app, NextVersion("kept", edits));

        File.ReadAllText(share).Should().Contain("the app's own change");
        app.Read("src/ContosoNotes/App.xaml.cs").Should().Contain("From the new template");
        output.Should().Contain("ShareService.cs").And.Contain("Skipped");
    }

    [TestMethod]
    public void Patch_FileTheTemplateDeletedAndTheAppKept_IsDeleted()
    {
        var app = CreateApp("tool-removed", "--platforms", "desktop");

        Patch(app, NextVersion("removed", new() { ["src/AppTemplate/Services/ShareService.cs"] = _ => null }));

        app.Exists("src/ContosoNotes/Services/ShareService.cs").Should().BeFalse();
    }

    [TestMethod]
    public void Patch_HostileGlobalGitConfig_StillWorks()
    {
        var app = CreateApp("tool-globalconfig", "--platforms", "desktop");
        var hooks = Path.Combine(TemplateFixture.WorkRoot, "hostile-hooks");
        Directory.CreateDirectory(hooks);
        File.WriteAllText(Path.Combine(hooks, "pre-commit"), "#!/bin/sh\nexit 1\n");
        var config = Path.Combine(TemplateFixture.WorkRoot, "hostile.gitconfig");
        File.WriteAllText(config, $"[diff]\n\tnoprefix = true\n[commit]\n\tgpgsign = true\n[core]\n\thooksPath = {hooks.Replace('\\', '/')}\n");

        ToolWithEnvironment(
            app.Root,
            new() { ["GIT_CONFIG_GLOBAL"] = config },
            "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", NextVersion("globalconfig", _appCodeEdit));

        app.Read("src/ContosoNotes/App.xaml.cs").Should().Contain("From the new template");
    }

    [TestMethod]
    public void Patch_AppUsesGitAttributes_DoesNotConflictOnLineEndings()
    {
        var app = CreateApp("tool-attributes", "--platforms", "desktop");
        TemplateFixture.Run("git", ["config", "core.autocrlf", "false"], app.Root);
        File.WriteAllText(app.Path(".gitattributes"), "* text=auto\n");
        TemplateFixture.Run("git", ["add", "--renormalize", "."], app.Root);
        Commit(app, "normalize");

        var output = Patch(app, NextVersion("attributes", _appCodeEdit));

        output.Should().Contain("cleanly");
        app.Read("src/ContosoNotes/App.xaml.cs").Should().NotContain("<<<<<<<");
    }

    [TestMethod]
    public void Patch_ConflictList_HasOnlyRealFileNames()
    {
        var app = CreateApp("tool-names", "--platforms", "desktop");
        var mainView = app.Path("src/ContosoNotes/Views/MainView.xaml");
        File.WriteAllText(mainView, File.ReadAllText(mainView).Replace("Hello Uno Platform!", "Hello from the app"));
        Commit(app, "app change");

        var output = Patch(app, NextVersion("names", new()
        {
            ["src/AppTemplate/Views/MainView.xaml"] = text => text.Replace("Hello Uno Platform!", "Hello from the template"),
        }));

        output.Should().NotContain("warning:");
    }

    [TestMethod]
    public void Patch_OlderToolThanTheApp_RefusesToGoBackwards()
    {
        var app = CreateApp("tool-downgrade", "--platforms", "desktop");
        var state = app.Path(".template-state.json");
        File.WriteAllText(state, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(state), "\"templateVersion\": \"[^\"]+\"", "\"templateVersion\": \"99.0.0\""));
        Commit(app, "from the future");

        var act = () => Patch(app, TemplateFixture.PackagePath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*newer*");
    }

    [TestMethod]
    public void New_GitHasNoIdentity_WarnsInsteadOfPretendingItCommitted()
    {
        var path = Path.Combine(TemplateFixture.WorkRoot, "tool-noidentity");
        var config = Path.Combine(TemplateFixture.WorkRoot, "noidentity.gitconfig");
        File.WriteAllText(config, "[user]\n\tuseConfigOnly = true\n");

        var output = ToolWithEnvironment(
            TemplateFixture.WorkRoot,
            new()
            {
                ["GIT_CONFIG_GLOBAL"] = config,
                ["GIT_AUTHOR_NAME"] = "",
                ["GIT_AUTHOR_EMAIL"] = "",
                ["GIT_COMMITTER_NAME"] = "",
                ["GIT_COMMITTER_EMAIL"] = "",
            },
            "new", "ContosoNotes", "-o", path, "--yes", "--platforms", "desktop", "--template-source", TemplateFixture.PackagePath);

        output.Should().Contain("first commit");
    }

    [TestMethod]
    public void Patch_NoTemplateChange_DoesNothing()
    {
        var app = CreateApp("tool-noop", "--platforms", "desktop");

        var output = Tool(app.Root, "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", TemplateFixture.PackagePath);

        output.Should().Contain("hasn't changed");
        TemplateFixture.Run("git", ["status", "--porcelain"], app.Root).Trim().Should().BeEmpty();
    }

    [TestMethod]
    public void Patch_DirtyWorkingTree_Refuses()
    {
        var app = CreateApp("tool-dirty", "--platforms", "desktop");
        File.WriteAllText(app.Path("scratch.txt"), "uncommitted");

        var act = () => Tool(app.Root, "upgrade", "--patch", "--from-source", TemplateFixture.PackagePath, "--template-source", TemplateFixture.PackagePath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Commit or stash*");
    }

    [TestMethod]
    public void Adopt_CreatesStateForAHandCopiedApp()
    {
        var app = CreateApp("tool-adopt", "--platforms", "windows", "--without", "share");
        File.Delete(app.Path(".template-state.json"));

        Tool(app.Root, "adopt", "--name", "ContosoNotes", "--title", "Contoso");

        using var state = JsonDocument.Parse(app.Read(".template-state.json"));
        var options = state.RootElement.GetProperty("options");
        options.GetProperty("platforms").GetString().Should().Be("windows");
        options.GetProperty("share").GetString().Should().Be("False");
        options.GetProperty("rating").GetString().Should().Be("True");
        options.GetProperty("title").GetString().Should().Be("Contoso");
        state.RootElement.GetProperty("appliedUpgrades").GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public void Features_ListsEveryFeature()
    {
        var output = Tool(TemplateFixture.WorkRoot, "features", "--json");

        using var manifest = JsonDocument.Parse(output);
        manifest.RootElement.GetProperty("shortName").GetString().Should().Be("mzuno-app");
    }

    [TestMethod]
    public void Errors_ReadLikeMessagesNotStackTraces()
    {
        var act = () => Tool(TemplateFixture.WorkRoot, "upgrade", "--path", TemplateFixture.WorkRoot);

        act.Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("mzuno-app adopt") && !e.Message.Contains("   at "));
    }

    [TestMethod]
    public void New_DryRun_PrintsTheCommandAndCreatesNothing()
    {
        var output = Tool(TemplateFixture.WorkRoot, "new", "--dry-run", "--yes", "DryRunProbe", "--without", "rating");

        output.Should().Contain("dotnet new mzuno-app -n DryRunProbe --rating false");
        Directory.Exists(Path.Combine(TemplateFixture.WorkRoot, "DryRunProbe")).Should().BeFalse();
    }

    [TestMethod]
    public void Bundled_ToolCarriesEveryNoteInTheRepository()
    {
        var app = CreateApp("tool-notes", "--platforms", "desktop");

        foreach (var note in Directory.GetFiles(Path.Combine(TemplateFixture.RepoRoot, "docs", "upgrades"), "2*.md"))
        {
            Tool(app.Root, "upgrade", "--show", Path.GetFileNameWithoutExtension(note)).Should().Contain("## Steps");
        }
    }

    [TestMethod]
    public void FreshApp_RecordsEveryPublishedNoteAsApplied()
    {
        var app = CreateApp("tool-stamped", "--platforms", "desktop");

        var state = app.Read(".template-state.json");
        foreach (var note in Directory.GetFiles(Path.Combine(TemplateFixture.RepoRoot, "docs", "upgrades"), "2*.md"))
        {
            state.Should().Contain(Path.GetFileNameWithoutExtension(note));
        }
    }
}
