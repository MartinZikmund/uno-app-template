using System.Diagnostics;
using System.Text;

namespace MZikmund.UnoApp.Cli;

/// <param name="StandardOutput">What to parse. Git's warnings go to stderr and must never be read as data.</param>
/// <param name="Output">Both streams together, for showing a person what went wrong.</param>
internal sealed record ProcessResult(int ExitCode, string StandardOutput, string Output)
{
    public bool Succeeded => ExitCode == 0;
}

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ProcessStartInfo info = new(fileName)
        {
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            info.Environment[key] = value;
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        StringBuilder standardOutput = new();
        StringBuilder combined = new();
        process.OutputDataReceived += (_, e) =>
        {
            lock (combined)
            {
                standardOutput.AppendLine(e.Data);
                combined.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            lock (combined)
            {
                combined.AppendLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, standardOutput.ToString().Trim(), combined.ToString().Trim());
    }

    /// <returns>Standard output only.</returns>
    public static async Task<string> RunOrThrowAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var list = arguments.ToList();
        var result = await RunAsync(fileName, list, workingDirectory, environment);
        if (!result.Succeeded)
        {
            throw new CliException($"{fileName} {string.Join(' ', list)} failed:\n{result.Output}");
        }

        return result.StandardOutput;
    }
}

/// <summary>An error meant for the person at the keyboard: shown as a message, without a stack trace.</summary>
internal sealed class CliException(string message) : Exception(message);
