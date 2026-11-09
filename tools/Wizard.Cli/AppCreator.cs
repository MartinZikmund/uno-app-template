using MZikmund.UnoApp.Wizard;

namespace MZikmund.UnoApp.Cli;

internal sealed record CreatedApp(string Path, string? Warning);

internal static class AppCreator
{
    public static async Task<CreatedApp> CreateAsync(WizardSelection selection, TemplateRunner runner, bool initializeGit)
    {
        var output = Path.GetFullPath(selection.OutputDirectory.Length > 0 ? selection.OutputDirectory : selection.Name);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
        {
            throw new CliException($"{output} already exists and isn't empty.");
        }

        selection.OutputDirectory = output;
        await runner.RunRawAsync(selection.ToDotnetNewArguments());

        var warning = initializeGit ? await InitializeGitAsync(output) : null;
        return new CreatedApp(output, warning);
    }

    private static async Task<string?> InitializeGitAsync(string directory)
    {
        var init = await ProcessRunner.RunAsync("git", ["init", "-q", "-b", "main"], directory);
        if (!init.Succeeded)
        {
            return "Couldn't create a git repository, so the app has none. It still builds.";
        }

        await ProcessRunner.RunAsync("git", ["add", "-A"], directory);
        var commit = await ProcessRunner.RunAsync("git", ["commit", "-q", "-m", "chore: create app from the Uno App template"], directory);
        return commit.Succeeded
            ? null
            : "The repository was created, but git couldn't make the first commit, usually because user.name and user.email " +
              "aren't set. Set them, then run: git commit -m \"Initial commit\"";
    }
}
