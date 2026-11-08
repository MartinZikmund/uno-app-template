using MZikmund.UnoApp.Wizard;

namespace MZikmund.UnoApp.Cli;

internal sealed record SkippedFile(string Path, string Reason);

internal sealed record PatchResult(
    IReadOnlyList<string> Merged,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<SkippedFile> Skipped,
    string ToVersion)
{
    public bool Changed => Merged.Count > 0 || Conflicts.Count > 0 || Skipped.Count > 0;
}

/// <summary>
/// Brings the template's own changes into an app: generates the old and new template output with the app's name and
/// options, diffs those, and three-way merges the difference. The app's edits survive, because git knows the common
/// ancestor of every file. Files merge one at a time, so a sample page the app deleted can't block everything else.
/// </summary>
internal static class TemplatePatcher
{
    public static async Task<PatchResult> ApplyAsync(string appRoot, TemplateState state, TemplateManifest manifest, TemplateRunner from, TemplateRunner to)
    {
        var status = await GitAsync(appRoot, "status", "--porcelain", "--", ".");
        if (status.Length > 0)
        {
            throw new CliException("Commit or stash your changes first, so the merge is easy to review and to undo.");
        }

        var topLevel = (await GitAsync(appRoot, "rev-parse", "--show-toplevel")).Trim();
        var prefix = (await GitAsync(appRoot, "rev-parse", "--show-prefix")).Trim();

        var work = Path.Combine(Path.GetTempPath(), "mzuno-patch-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(work);
        try
        {
            var options = state.ToDotnetNewOptionArguments(manifest);
            var targetTree = Path.Combine(work, "target");
            await to.GenerateAsync(state.Name, targetTree, options);
            var targetState = TemplateState.Parse(await File.ReadAllTextAsync(Path.Combine(targetTree, TemplateState.FileName)));
            if (TemplateState.IsOlder(targetState.TemplateVersion, state.TemplateVersion))
            {
                throw new CliException(
                    $"The app is on a newer template ({state.TemplateVersion}) than this tool carries ({targetState.TemplateVersion}). " +
                    "Update the tool: dotnet tool update -g MZikmund.UnoApp.Cli");
            }

            var baseTree = Path.Combine(work, "base");
            await from.GenerateAsync(state.Name, baseTree, options);

            // The state file is the one thing the app owns outright, so it stays out of the merge.
            File.Delete(Path.Combine(baseTree, TemplateState.FileName));
            File.Delete(Path.Combine(targetTree, TemplateState.FileName));

            TemporaryRepository repository = await TemporaryRepository.CreateAsync(work, appRoot, topLevel);
            await repository.CommitAsync(baseTree, "base");
            await repository.CommitAsync(targetTree, "target");

            var changes = await repository.ChangesAsync();
            if (changes.Count == 0)
            {
                if (targetState.TemplateVersion != state.TemplateVersion)
                {
                    await WriteStateAsync(appRoot, state, targetState);
                }

                return new PatchResult([], [], [], targetState.TemplateVersion);
            }

            // Bringing the objects over is what lets --3way find each file's common ancestor.
            await ProcessRunner.RunOrThrowAsync("git", ["fetch", "-q", "--no-tags", repository.Root, "HEAD"], appRoot);

            List<string> merged = [];
            List<string> conflicts = [];
            List<SkippedFile> skipped = [];
            foreach (var (kind, path) in changes)
            {
                var patch = await repository.PatchForAsync(path, work);
                string[] arguments = prefix.Length > 0
                    ? ["apply", "--3way", "--whitespace=nowarn", $"--directory={prefix}", patch]
                    : ["apply", "--3way", "--whitespace=nowarn", patch];

                // From the top level, because git apply silently ignores paths outside the current folder and exits 0.
                var apply = await ProcessRunner.RunAsync("git", arguments, topLevel);
                if (apply.Succeeded)
                {
                    merged.Add(path);
                    continue;
                }

                var unmerged = await GitAsync(topLevel, "diff", "--name-only", "--diff-filter=U", "--", prefix + path);
                if (unmerged.Length > 0)
                {
                    conflicts.Add(path);
                }
                else
                {
                    skipped.Add(new SkippedFile(path, ReasonFor(kind, File.Exists(Path.Combine(appRoot, path)))));
                }
            }

            await WriteStateAsync(appRoot, state, targetState);
            return new PatchResult(merged, conflicts, skipped, targetState.TemplateVersion);
        }
        finally
        {
            TryDelete(work);
        }
    }

    private static string ReasonFor(char kind, bool existsInApp) => (kind, existsInApp) switch
    {
        ('D', true) => "the template removed it, but your copy has changed, so it was kept",
        ('A', true) => "the template added it, but you already have a file with that name",
        ('M', false) => "the template changed it, but your app no longer has it",
        _ => "the change couldn't be applied, so compare it by hand",
    };

    private static Task WriteStateAsync(string appRoot, TemplateState state, TemplateState targetState)
    {
        // A note applied earlier stays applied, and options the new version added take the value it was generated with.
        Dictionary<string, string> options = new(targetState.Options);
        foreach (var option in state.Options)
        {
            options[option.Key] = option.Value;
        }

        var updated = state with { TemplateVersion = targetState.TemplateVersion, Options = options };
        return File.WriteAllTextAsync(Path.Combine(appRoot, TemplateState.FileName), updated.ToJson());
    }

    private static async Task<string> GitAsync(string directory, params string[] arguments) =>
        await ProcessRunner.RunOrThrowAsync("git", arguments, directory);

    private static void TryDelete(string directory)
    {
        try
        {
            ForceDelete(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Only a temp folder.
        }
    }

    internal static void ForceDelete(string directory)
    {
        // Git marks its objects read-only, and Directory.Delete refuses those on Windows.
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(directory, recursive: true);
    }

    /// <summary>
    /// Holds the two template trees. The developer's global git config stays out, because settings like
    /// diff.noprefix, commit.gpgsign or a global hook would change or block what happens here. Only what affects how
    /// blobs are normalized is copied from the app, or every line would differ by its ending and conflict.
    /// </summary>
    private sealed class TemporaryRepository
    {
        private readonly Dictionary<string, string> _environment;

        private TemporaryRepository(string root, Dictionary<string, string> environment)
        {
            Root = root;
            _environment = environment;
        }

        public string Root { get; }

        public static async Task<TemporaryRepository> CreateAsync(string work, string appRoot, string topLevel)
        {
            var emptyConfig = Path.Combine(work, "empty.gitconfig");
            await File.WriteAllTextAsync(emptyConfig, "");
            Dictionary<string, string> environment = new()
            {
                ["GIT_CONFIG_GLOBAL"] = emptyConfig,
                ["GIT_CONFIG_SYSTEM"] = emptyConfig,
                ["GIT_CONFIG_NOSYSTEM"] = "1",
            };

            var root = Path.Combine(work, "repo");
            Directory.CreateDirectory(root);
            TemporaryRepository repository = new(root, environment);
            await repository.GitAsync("init", "-q");

            foreach (var key in new[] { "core.autocrlf", "core.eol" })
            {
                // Asked of the app's repository with the developer's own config in place, since that's what it uses.
                var value = await ProcessRunner.RunAsync("git", ["config", "--get", key], appRoot);
                if (value.Succeeded && value.StandardOutput.Length > 0)
                {
                    await repository.GitAsync("config", key, value.StandardOutput.Trim());
                }
            }

            await repository.GitAsync("config", "core.safecrlf", "false");

            // Attributes outrank core.autocrlf, and the trees being committed don't carry the app's own file.
            var attributes = new[] { Path.Combine(topLevel, ".gitattributes"), Path.Combine(appRoot, ".gitattributes") }
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .Select(File.ReadAllText);
            var info = Path.Combine(root, ".git", "info");
            Directory.CreateDirectory(info);
            await File.WriteAllTextAsync(Path.Combine(info, "attributes"), string.Join('\n', attributes));

            return repository;
        }

        public async Task CommitAsync(string tree, string message)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(Root).Where(e => Path.GetFileName(e) != ".git"))
            {
                if (Directory.Exists(entry))
                {
                    ForceDelete(entry);
                }
                else
                {
                    File.Delete(entry);
                }
            }

            foreach (var file in Directory.EnumerateFiles(tree, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(Root, Path.GetRelativePath(tree, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }

            await GitAsync("add", "-A", "--force");
            await GitAsync("-c", "user.name=mzuno-app", "-c", "user.email=mzuno-app@localhost", "commit", "-q", "--allow-empty", "--no-verify", "-m", message);
        }

        public async Task<IReadOnlyList<(char Kind, string Path)>> ChangesAsync()
        {
            var output = await GitAsync("-c", "core.quotepath=off", "diff", "--name-status", "--no-renames", "--no-ext-diff", "HEAD~1", "HEAD");
            return
            [
                .. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(line => line.Split('\t', 2))
                    .Select(parts => (parts[0][0], parts[1])),
            ];
        }

        public async Task<string> PatchForAsync(string path, string work)
        {
            var patch = Path.Combine(work, "file.patch");
            await GitAsync(
                "diff", "--full-index", "--binary", "--no-renames", "--no-ext-diff", "--no-color", "--src-prefix=a/", "--dst-prefix=b/",
                "--output", patch, "HEAD~1", "HEAD", "--", path);
            return patch;
        }

        private async Task<string> GitAsync(params string[] arguments) =>
            await ProcessRunner.RunOrThrowAsync("git", arguments, Root, _environment);
    }
}
