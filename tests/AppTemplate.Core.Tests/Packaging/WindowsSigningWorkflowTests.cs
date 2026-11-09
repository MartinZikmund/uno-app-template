using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class WindowsSigningWorkflowTests
{
    private static string Script => GetSigningScript();

    // `winapp sign` has no --cert-password; the unknown option made it fail, and the failure was ignored.
    [TestMethod]
    public void SignStep_UsesWinappSignSyntax()
    {
        Script.Should().NotContain("--cert-password");
        Script.Should().MatchRegex(@"winapp sign ""[^""]+"" \$pfx --password ");
    }

    // A password-less Get-PfxCertificate prompts on stdin and hangs a CI job forever.
    [TestMethod]
    public void SignStep_NeverPromptsForThePfxPassword()
    {
        Script.Should().Contain("Get-PfxCertificate");
        Script.Should().MatchRegex(@"Get-PfxCertificate [^\r\n]*-Password ");
    }

    [TestMethod]
    public void SignStep_StopsOnNativeFailure()
    {
        Script.Should().Contain("$PSNativeCommandUseErrorActionPreference = $true");
    }

    [TestMethod]
    public void SignStep_HasATimeout()
    {
        GetSigningStep().Should().MatchRegex(@"timeout-minutes: \d+");
    }

    private static string GetSigningStep()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github", "workflows", "_build-windows.yml"));
        var start = yaml.IndexOf("- name: Sign the Dev bundle", StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var end = yaml.IndexOf("\n      - ", start + 1, StringComparison.Ordinal);
        return yaml[start..end];
    }

    private static string GetSigningScript()
    {
        var step = GetSigningStep();
        return step[(step.IndexOf("run: |", StringComparison.Ordinal))..];
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (AGENTS.md) not found.");
    }
}
