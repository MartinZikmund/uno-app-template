using System.Diagnostics;
using System.Text;

namespace Template.GenerationTests;

[TestClass]
public static class TemplateFixture
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string WorkRoot { get; private set; } = "";

    public static string PackagePath { get; private set; } = "";

    private static string Hive => Path.Combine(WorkRoot, "hive");

    [AssemblyInitialize]
    public static void PackAndInstall(TestContext _)
    {
        // A short path: generated apps get long once Uno's obj folders are involved.
        WorkRoot = Path.Combine(Path.GetTempPath(), "mzu" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(WorkRoot);

        var packageDir = Path.Combine(WorkRoot, "pkg");
        Run("dotnet", ["pack", Path.Combine(RepoRoot, "tools", "TemplatePack", "MZikmund.UnoApp.Templates.csproj"), "-o", packageDir, "-c", "Release"], RepoRoot);
        PackagePath = Directory.GetFiles(packageDir, "*.nupkg").Single();
        Run("dotnet", ["new", "install", PackagePath, "--debug:custom-hive", Hive], WorkRoot);
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        try
        {
            Directory.Delete(WorkRoot, recursive: true);
        }
        catch (IOException)
        {
            // Build servers can hold files for a moment. It's only a temp folder.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static GeneratedApp Generate(string scenario, string name, params string[] options)
    {
        var output = Path.Combine(WorkRoot, scenario);
        Run("dotnet", ["new", "mzuno-app", "-n", name, "-o", output, "--debug:custom-hive", Hive, .. options], WorkRoot);
        return new GeneratedApp(output, name);
    }

    public static string Run(string fileName, IEnumerable<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment = null)
    {
        ProcessStartInfo info = new(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        info.Environment["GIT_AUTHOR_NAME"] = "Test";
        info.Environment["GIT_AUTHOR_EMAIL"] = "test@example.com";
        info.Environment["GIT_COMMITTER_NAME"] = "Test";
        info.Environment["GIT_COMMITTER_EMAIL"] = "test@example.com";
        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            info.Environment[key] = value;
        }


        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        StringBuilder log = new();
        process.OutputDataReceived += (_, e) => { lock (log) { log.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (log) { log.AppendLine(e.Data); } };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} {string.Join(' ', arguments)} exited with {process.ExitCode}:\n{Tail(log.ToString())}");
        }

        return log.ToString();
    }

    private static string Tail(string text) => text.Length <= 6000 ? text : text[^6000..];

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".template.config", "template.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Template root not found.");
    }
}

public sealed record GeneratedApp(string Root, string Name)
{
    public string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public bool Exists(string relative) => File.Exists(Path(relative)) || Directory.Exists(Path(relative));

    public string Read(string relative) => File.ReadAllText(Path(relative));

    public IEnumerable<string> AllFiles() =>
        Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
            .Select(f => System.IO.Path.GetRelativePath(Root, f).Replace('\\', '/'))
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.StartsWith(".git/"));

    public IEnumerable<string> TextFiles() =>
        AllFiles().Where(f => System.IO.Path.GetExtension(f) is not (".png" or ".ico" or ".ttf" or ".svg" or ".jpg"));
}
