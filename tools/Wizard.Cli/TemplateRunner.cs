using System.Security.Cryptography;
using System.Text;

namespace MZikmund.UnoApp.Cli;

/// <summary>
/// Runs <c>dotnet new</c> against one template version in a private hive, so nothing changes in the user's own
/// template list and two versions can coexist, which <c>upgrade --patch</c> depends on.
/// </summary>
internal sealed class TemplateRunner
{
    private readonly string _source;
    private readonly string _hive;

    public TemplateRunner(string source)
    {
        _source = source;
        var key = File.Exists(source) ? $"{Path.GetFullPath(source)}|{File.GetLastWriteTimeUtc(source):O}" : source;
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        _hive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mzuno-app", "hives", hash);
    }

    public static TemplateRunner ForCurrentVersion(string? explicitSource)
    {
        var source = explicitSource ?? Bundled.TemplatePackage()
            ?? throw new CliException("This build has no bundled template. Pass --template-source <path to .nupkg>.");
        return new TemplateRunner(source);
    }

    public async Task GenerateAsync(string name, string outputDirectory, IEnumerable<string> optionArguments)
    {
        await EnsureInstalledAsync();
        await ProcessRunner.RunOrThrowAsync(
            "dotnet",
            ["new", "mzuno-app", "-n", name, "-o", outputDirectory, "--debug:custom-hive", _hive, .. optionArguments]);
    }

    public async Task RunRawAsync(IEnumerable<string> newArguments)
    {
        await EnsureInstalledAsync();
        await ProcessRunner.RunOrThrowAsync("dotnet", [.. newArguments, "--debug:custom-hive", _hive]);
    }

    private async Task EnsureInstalledAsync()
    {
        var marker = Path.Combine(_hive, ".installed");
        if (File.Exists(marker))
        {
            return;
        }

        Directory.CreateDirectory(_hive);
        var result = await ProcessRunner.RunAsync("dotnet", ["new", "install", _source, "--force", "--debug:custom-hive", _hive]);
        if (!result.Succeeded)
        {
            throw new CliException(
                $"Could not install the template from {_source}. If that version was never published, " +
                $"pass its package with --from-source <path to .nupkg>.\n{result.Output}");
        }

        await File.WriteAllTextAsync(marker, _source);
    }
}
