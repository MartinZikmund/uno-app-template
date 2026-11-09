using System.CommandLine;
using System.Text.Json;
using MZikmund.UnoApp.Cli;
using MZikmund.UnoApp.Wizard;
using Spectre.Console;

RootCommand root = new("Create Uno Platform apps from the Uno App template, and keep them up to date.");
root.Subcommands.Add(BuildNew());
root.Subcommands.Add(BuildWizard());
root.Subcommands.Add(BuildFeatures());
root.Subcommands.Add(BuildUpgrade());
root.Subcommands.Add(BuildAdopt());
root.Subcommands.Add(BuildCheckUpgradeNote());
root.Subcommands.Add(BuildWritePreview());

try
{
    // Off so CliException reaches the handler below instead of printing as a stack trace.
    return await root.Parse(args).InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
}
catch (CliException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static Command BuildNew()
{
    Argument<string?> name = new("name") { Description = "Project name.", Arity = ArgumentArity.ZeroOrOne };
    Option<string?> output = new("--output", "-o") { Description = "Folder to create. Defaults to ./<name>." };
    Option<bool> yes = new("--yes", "-y") { Description = "Don't ask anything: take the defaults plus whatever was passed." };
    Option<string[]> platforms = new("--platforms") { Description = "Platforms to include. Defaults to all.", AllowMultipleArgumentsPerToken = true };
    Option<string[]> with = new("--with") { Description = "Features to switch on.", AllowMultipleArgumentsPerToken = true };
    Option<string[]> without = new("--without") { Description = "Features to switch off.", AllowMultipleArgumentsPerToken = true };
    Option<string?> title = new("--title") { Description = "Display name." };
    Option<string?> appId = new("--app-id") { Description = "Reverse-DNS application id." };
    Option<string?> publisher = new("--publisher") { Description = "Your name or company." };
    Option<string?> githubRepo = new("--github-repo") { Description = "owner/repo." };
    Option<bool> noGit = new("--no-git") { Description = "Don't create a git repository." };
    Option<bool> dryRun = new("--dry-run") { Description = "Print the dotnet new command instead of running it." };
    Option<string?> source = new("--template-source") { Description = "Template package to use instead of the bundled one." };

    Command command = new("new", "Create an app, asking for anything you didn't pass.")
    {
        name, output, yes, platforms, with, without, title, appId, publisher, githubRepo, noGit, dryRun, source,
    };
    command.SetAction(async parse =>
    {
        var manifest = Bundled.Manifest();
        WizardSelection selection = new(manifest) { Name = parse.GetValue(name) ?? "", OutputDirectory = parse.GetValue(output) ?? "" };

        SetIfPresent(selection, "title", parse.GetValue(title));
        SetIfPresent(selection, "appId", parse.GetValue(appId));
        SetIfPresent(selection, "publisher", parse.GetValue(publisher));
        SetIfPresent(selection, "githubRepo", parse.GetValue(githubRepo));

        var chosenPlatforms = Split(parse.GetValue(platforms));
        ApplyPlatforms(manifest, selection, chosenPlatforms);
        var featureFlagsGiven = ApplyFeatureFlags(manifest, selection, Split(parse.GetValue(with)), Split(parse.GetValue(without)));

        var interactive = !parse.GetValue(yes) && !Console.IsInputRedirected && !Console.IsOutputRedirected;
        if (interactive)
        {
            if (!InteractiveWizard.Run(AnsiConsole.Console, manifest, selection, askFeatures: !featureFlagsGiven, askPlatforms: chosenPlatforms.Count == 0))
            {
                return 0;
            }
        }

        var problems = selection.Validate();
        if (problems.Count > 0)
        {
            throw new CliException(string.Join(Environment.NewLine, problems));
        }

        if (parse.GetValue(dryRun))
        {
            Console.WriteLine(selection.ToCommandLine());
            return 0;
        }

        var created = await AppCreator.CreateAsync(selection, TemplateRunner.ForCurrentVersion(parse.GetValue(source)), !parse.GetValue(noGit));
        Console.WriteLine($"Created {selection.Name} in {created.Path}");
        if (created.Warning is not null)
        {
            Console.WriteLine(created.Warning);
        }

        Console.WriteLine($"Next: dotnet build {Path.Combine(created.Path, "src", selection.Name, selection.Name + ".csproj")} -f <target framework>");
        return 0;
    });
    return command;
}

static Command BuildWizard()
{
    Option<int> port = new("--port") { Description = "Port to listen on. Defaults to a free one." };
    Option<bool> noBrowser = new("--no-browser") { Description = "Only print the address." };
    Option<string?> uiRoot = new("--ui-root") { Description = "Published UI folder to serve instead of the bundled one." };
    Option<string?> outputRoot = new("--output-root") { Description = "Where new apps go by default. Defaults to the current folder." };
    Option<string?> source = new("--template-source") { Description = "Template package to use instead of the bundled one." };

    Command command = new("wizard", "Open the visual wizard in your browser.") { port, noBrowser, uiRoot, outputRoot, source };
    command.SetAction(async (parse, cancellationToken) =>
    {
        var ui = parse.GetValue(uiRoot) ?? Bundled.UiRoot;
        if (!File.Exists(Path.Combine(ui, "index.html")))
        {
            throw new CliException($"No wizard UI found in {ui}. Use 'mzuno-app new', or pass --ui-root.");
        }

        WizardServer server = new(
            Bundled.Manifest(),
            TemplateRunner.ForCurrentVersion(parse.GetValue(source)),
            Path.GetFullPath(ui),
            parse.GetValue(outputRoot) ?? Environment.CurrentDirectory);
        await server.RunAsync(parse.GetValue(port), !parse.GetValue(noBrowser), cancellationToken);
        return 0;
    });
    return command;
}

static Command BuildFeatures()
{
    Option<bool> json = new("--json") { Description = "Print the whole manifest as JSON." };
    Command command = new("features", "List the platforms and features you can pick.") { json };
    command.SetAction(parse =>
    {
        var manifest = Bundled.Manifest();
        if (parse.GetValue(json))
        {
            Console.WriteLine(JsonSerializer.Serialize(manifest, WizardJsonContext.Default.TemplateManifest));
            return 0;
        }

        Table table = new();
        table.AddColumn("Option").AddColumn("What you get");
        table.AddRow(new Markup("[bold]Platforms[/]"), new Text(""));
        foreach (var platform in manifest.Platforms)
        {
            table.AddRow(new Text(platform.Id), new Text(platform.Description));
        }

        foreach (var group in manifest.Groups)
        {
            table.AddRow(new Markup($"[bold]{Markup.Escape(group.Title)}[/]"), new Text(""));
            foreach (var feature in group.Features)
            {
                table.AddRow(new Text(feature.CliName), new Text(feature.Description));
            }
        }

        AnsiConsole.Write(table);
        return 0;
    });
    return command;
}

static Command BuildUpgrade()
{
    Option<string> path = new("--path") { Description = "App repository root.", DefaultValueFactory = _ => "." };
    Option<bool> json = new("--json") { Description = "Print pending notes as JSON." };
    Option<string?> show = new("--show") { Description = "Print one note." };
    Option<string[]> markApplied = new("--mark-applied") { Description = "Record notes as applied.", AllowMultipleArgumentsPerToken = true };
    Option<bool> markAll = new("--mark-all-applied") { Description = "Record every pending note as applied." };
    Option<bool> patch = new("--patch") { Description = "Three-way merge the template's own changes into the working tree." };
    Option<string?> from = new("--from-source") { Description = "Template package for the app's current version. Defaults to that version on NuGet." };
    Option<string?> to = new("--template-source") { Description = "Template package to upgrade to. Defaults to the bundled one." };

    Command command = new("upgrade", "See and apply what newer template versions bring.") { path, json, show, markApplied, markAll, patch, from, to };
    command.SetAction(async parse =>
    {
        var root = Path.GetFullPath(parse.GetValue(path)!);
        var statePath = Path.Combine(root, TemplateState.FileName);
        if (!File.Exists(statePath))
        {
            throw new CliException($"No {TemplateState.FileName} in {root}. For an app copied by hand, run 'mzuno-app adopt' first.");
        }

        var state = TemplateState.Parse(await File.ReadAllTextAsync(statePath));
        var catalog = Bundled.UpgradeNotes();

        if (parse.GetValue(show) is { } id)
        {
            var note = catalog.FirstOrDefault(n => n.Id == id) ?? throw new CliException($"No upgrade note named {id}.");
            Console.WriteLine($"{note.Title} ({note.Impact.ToString().ToLowerInvariant()})");
            Console.WriteLine();
            Console.WriteLine(note.Body);
            return 0;
        }

        var toMark = parse.GetValue(markApplied) ?? [];
        if (toMark.Length > 0 || parse.GetValue(markAll))
        {
            var unknown = toMark.Except(catalog.Select(n => n.Id)).ToList();
            if (unknown.Count > 0)
            {
                throw new CliException($"Unknown upgrade note(s): {string.Join(", ", unknown)}.");
            }

            var ids = parse.GetValue(markAll) ? UpgradePlanner.Pending(catalog, state).Select(n => n.Id) : toMark;
            await File.WriteAllTextAsync(statePath, state.MarkApplied(ids).ToJson());
            Console.WriteLine($"Updated {TemplateState.FileName}.");
            return 0;
        }

        if (parse.GetValue(patch))
        {
            var fromSource = parse.GetValue(from) ?? $"{TemplateState.TemplatePackageId}::{state.TemplateVersion}";
            var result = await TemplatePatcher.ApplyAsync(root, state, Bundled.Manifest(), new TemplateRunner(fromSource), TemplateRunner.ForCurrentVersion(parse.GetValue(to)));
            if (!result.Changed)
            {
                Console.WriteLine("The template's output hasn't changed for this app's options.");
                return 0;
            }

            if (result.Conflicts.Count == 0 && result.Skipped.Count == 0)
            {
                Console.WriteLine($"Merged template {result.ToVersion} cleanly ({result.Merged.Count} files). Review the changes in your working tree.");
            }
            else
            {
                Console.WriteLine($"Merged {result.Merged.Count} files from template {result.ToVersion}.");
            }

            if (result.Conflicts.Count > 0)
            {
                Console.WriteLine("Resolve the conflict markers in:");
                foreach (var conflict in result.Conflicts)
                {
                    Console.WriteLine($"  {conflict}");
                }
            }

            if (result.Skipped.Count > 0)
            {
                Console.WriteLine("Skipped, so check whether these matter to you:");
                foreach (var file in result.Skipped)
                {
                    Console.WriteLine($"  {file.Path}: {file.Reason}");
                }
            }

            Console.WriteLine("Now walk the pending notes: they carry manual steps a merge can't do.");
            return 0;
        }

        var pending = UpgradePlanner.Pending(catalog, state);
        if (parse.GetValue(json))
        {
            Console.WriteLine(JsonSerializer.Serialize(pending, WizardJsonContext.Default.IReadOnlyListUpgradeNote));
            return 0;
        }

        if (pending.Count == 0)
        {
            Console.WriteLine($"Nothing pending. The app is on template {state.TemplateVersion}.");
            return 0;
        }

        Console.WriteLine($"{pending.Count} upgrade note(s) pending for {state.Name}, oldest first:");
        foreach (var note in pending)
        {
            Console.WriteLine($"  [{note.Impact.ToString().ToLowerInvariant(),-11}] {note.Id}");
            Console.WriteLine($"                {note.Title}");
        }

        Console.WriteLine();
        Console.WriteLine("Read one with --show <id>, record it with --mark-applied <id>. --patch merges the mechanical changes.");
        return 0;
    });
    return command;
}

static Command BuildAdopt()
{
    Option<string> path = new("--path") { Description = "App repository root.", DefaultValueFactory = _ => "." };
    Option<string> name = new("--name") { Description = "The app's name, which replaced AppTemplate.", Required = true };
    Option<string?> title = new("--title") { Description = "Display name." };
    Option<string?> appId = new("--app-id") { Description = "Application id." };
    Option<string?> publisher = new("--publisher") { Description = "Publisher." };
    Option<string?> baseVersion = new("--template-version") { Description = "Template version the app is closest to, if you know it." };

    Command command = new("adopt", "Create .template-state.json for an app copied from the template by hand.") { path, name, title, appId, publisher, baseVersion };
    command.SetAction(async parse =>
    {
        var root = Path.GetFullPath(parse.GetValue(path)!);
        var statePath = Path.Combine(root, TemplateState.FileName);
        if (File.Exists(statePath))
        {
            throw new CliException($"{TemplateState.FileName} already exists.");
        }

        List<string> files = [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.StartsWith(".git/", StringComparison.Ordinal) && !file.Contains("/bin/") && !file.Contains("/obj/"))];

        var options = FeatureDetector.Detect(files, relative =>
        {
            var full = Path.Combine(root, relative);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        });
        foreach (var (key, option) in new[] { ("title", title), ("appId", appId), ("publisher", publisher) })
        {
            if (parse.GetValue(option) is { Length: > 0 } value)
            {
                options[key] = value;
            }
        }

        TemplateState state = new(TemplateState.TemplatePackageId, parse.GetValue(baseVersion) ?? "unknown", TemplateState.DefaultUpgradesUrl, parse.GetValue(name)!, options, []);
        await File.WriteAllTextAsync(statePath, state.ToJson());

        Console.WriteLine($"Wrote {statePath}. Review it: the features were inferred from your files.");
        foreach (var option in options)
        {
            Console.WriteLine($"  {option.Key}: {option.Value}");
        }

        return 0;
    });
    return command;
}

static Command BuildCheckUpgradeNote()
{
    Option<string> baseRef = new("--base") { Description = "Git ref to compare against.", Required = true };
    Option<string?> bodyFile = new("--pr-body-file") { Description = "File holding the pull request description." };

    // For CI on the template repository, which is why it isn't listed in --help.
    Command command = new("check-upgrade-note", "Fail when a change reaches apps without an upgrade note.") { baseRef, bodyFile };
    command.Hidden = true;
    command.SetAction(async parse =>
    {
        // --no-renames, or moving src/x to docs/y would list only the exempt destination.
        var diff = await ProcessRunner.RunOrThrowAsync("git", ["-c", "core.quotepath=off", "diff", "--name-status", "--no-renames", $"{parse.GetValue(baseRef)}...HEAD"]);
        var changes = diff.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(line => line.Split('\t', 2)).ToList();
        var body = parse.GetValue(bodyFile) is { } file && File.Exists(file) ? await File.ReadAllTextAsync(file) : null;

        var verdict = UpgradeNoteGate.Evaluate(
            changes.Select(change => change[1]),
            changes.Where(change => change[0] == "A").Select(change => change[1]),
            body);
        Console.WriteLine(verdict.Message);
        if (!verdict.Passed)
        {
            Console.WriteLine("Files that reach apps:");
            foreach (var shipped in verdict.ShippingFiles)
            {
                Console.WriteLine($"  {shipped}");
            }
        }

        return verdict.Passed ? 0 : 1;
    });
    return command;
}

static Command BuildWritePreview()
{
    Option<string> output = new("--output") { Description = "The published wizard UI's web root.", Required = true };

    // For CI on the template repository, which is why it isn't listed in --help.
    Command command = new("write-preview", "Let a published wizard UI run without a server, as a read-only preview.") { output };
    command.Hidden = true;
    command.SetAction(parse =>
    {
        try
        {
            PreviewSite.Write(parse.GetValue(output)!, Bundled.Manifest(), Bundled.Version);
        }
        catch (DirectoryNotFoundException exception)
        {
            throw new CliException(exception.Message);
        }

        return 0;
    });
    return command;
}

static IReadOnlyList<string> Split(string[]? values) =>
    [.. (values ?? []).SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];

static void SetIfPresent(WizardSelection selection, string parameter, string? value)
{
    if (!string.IsNullOrEmpty(value))
    {
        selection.SetParameter(parameter, value);
    }
}

static void ApplyPlatforms(TemplateManifest manifest, WizardSelection selection, IReadOnlyList<string> chosen)
{
    if (chosen.Count == 0)
    {
        return;
    }

    var unknown = chosen.Where(id => manifest.Platforms.All(p => p.Id != id)).ToList();
    if (unknown.Count > 0)
    {
        throw new CliException($"Unknown platform(s): {string.Join(", ", unknown)}. Choose from {string.Join(", ", manifest.Platforms.Select(p => p.Id))}.");
    }

    foreach (var platform in manifest.Platforms)
    {
        selection.SetPlatform(platform.Id, chosen.Contains(platform.Id));
    }
}

static bool ApplyFeatureFlags(TemplateManifest manifest, WizardSelection selection, IReadOnlyList<string> with, IReadOnlyList<string> without)
{
    if (with.Count == 0 && without.Count == 0)
    {
        return false;
    }

    string Resolve(string name) => manifest.FindFeature(name)?.Id
        ?? throw new CliException($"Unknown feature '{name}'. Run 'mzuno-app features' for the list.");

    var off = without.Select(Resolve).ToHashSet();
    List<string> wanted = [.. selection.Features.Where(feature => !off.Contains(feature)), .. with.Select(Resolve)];
    foreach (var kept in selection.SetFeatures(wanted))
    {
        Console.WriteLine($"Keeping {kept}: {string.Join(", ", selection.RequiredBy(kept))} needs it.");
    }

    return true;
}
