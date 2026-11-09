using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public partial class ProdBuildGuardTests
{
    private const string Publisher = "CN=Contoso Ltd., O=Contoso Ltd., C=CZ";

    private static readonly XNamespace _foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

    [TestMethod]
    [DataRow("0.1.42", "0")]
    [DataRow("0.1.42", "100042")]
    [DataRow("1.0.0", "1")]
    [DataRow("0.0.1", "1")]
    public void AssertStoreVersionStamped_VersionInjected_Passes(string displayVersion, string version)
    {
        var result = RunTarget("AssertStoreVersionStamped", ProdCi("-p:CiVersionInjected=true", $"-p:ApplicationDisplayVersion={displayVersion}", $"-p:ApplicationVersion={version}"));

        result.ExitCode.Should().Be(0, result.Output);
    }

    [TestMethod]
    public void AssertStoreVersionStamped_ApplicationVersionNotInjected_Fails()
    {
        var result = RunTarget("AssertStoreVersionStamped", ProdCi("-p:CiVersionInjected=true", "-p:ApplicationDisplayVersion=0.1.42"));

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("ApplicationVersion was not passed");
    }

    [TestMethod]
    public void AssertStoreVersionStamped_DisplayVersionNotInjected_Fails()
    {
        var result = RunTarget("AssertStoreVersionStamped", ProdCi("-p:CiVersionInjected=true", "-p:ApplicationVersion=0"));

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("ApplicationDisplayVersion was not passed");
    }

    [TestMethod]
    public void AssertStoreVersionStamped_CiVersionInjectedMissing_Fails()
    {
        var result = RunTarget("AssertStoreVersionStamped", ProdCi("-p:ApplicationDisplayVersion=0.1.42", "-p:ApplicationVersion=0"));

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("CiVersionInjected");
    }

    [TestMethod]
    public void AssertStoreVersionStamped_DevChannel_DoesNotRequireInjection()
    {
        var result = RunTarget("AssertStoreVersionStamped", ["-p:ContinuousIntegrationBuild=true", "-p:AppChannel=Dev"]);

        result.ExitCode.Should().Be(0, result.Output);
    }

    [TestMethod]
    [DataRow("Contoso.App", "", "")]
    [DataRow("", Publisher, "")]
    [DataRow("", "", "Contoso Ltd.")]
    [DataRow("Contoso.App", Publisher, "")]
    [DataRow("", Publisher, "Contoso Ltd.")]
    [DataRow("Contoso.App", "", "Contoso Ltd.")]
    public void StampWindowsStoreIdentity_PartialIdentity_FailsAndLeavesManifestAlone(string name, string publisher, string displayName)
    {
        var result = RunTarget("StampWindowsStoreIdentity", WindowsProd, StoreIdentity(name, publisher, displayName));

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("Partial Windows Store identity");
        result.Identity.Should().Be(("Original", "CN=Original", "Original Ltd."));
    }

    [TestMethod]
    public void StampWindowsStoreIdentity_NoIdentity_LeavesManifestAlone()
    {
        var result = RunTarget("StampWindowsStoreIdentity", WindowsProd);

        result.ExitCode.Should().Be(0, result.Output);
        result.Identity.Should().Be(("Original", "CN=Original", "Original Ltd."));
    }

    [TestMethod]
    public void StampWindowsStoreIdentity_FullIdentity_StampsAllThree()
    {
        var result = RunTarget("StampWindowsStoreIdentity", WindowsProd, StoreIdentity("Contoso.App", Publisher, "Contoso Ltd."));

        result.ExitCode.Should().Be(0, result.Output);
        result.Identity.Should().Be(("Contoso.App", Publisher, "Contoso Ltd."));
    }

    [TestMethod]
    public void StampWindowsStoreIdentity_DevChannel_IgnoresPartialIdentity()
    {
        var result = RunTarget("StampWindowsStoreIdentity", ["-p:TargetPlatformIdentifier=windows", "-p:AppChannel=Dev"], StoreIdentity("Contoso.App", "", ""));

        result.ExitCode.Should().Be(0, result.Output);
        result.Identity.Should().Be(("Original", "CN=Original", "Original Ltd."));
    }

    [TestMethod]
    [DataRow("_build-android.yml")]
    [DataRow("_build-ios.yml")]
    [DataRow("_build-tvos.yml")]
    [DataRow("_build-windows.yml")]
    [DataRow("_build-desktop.yml")]
    [DataRow("_build-wasm.yml")]
    public void BuildWorkflow_EveryPackagingCommand_InjectsTheVersion(string workflow)
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github", "workflows", workflow));

        var commands = PackagingCommand().Matches(yaml).Select(match => match.Value).ToList();

        commands.Should().NotBeEmpty();
        commands.Should().AllSatisfy(command => command.Should()
            .Contain("-p:CiVersionInjected=true")
            .And.Contain("-p:ApplicationDisplayVersion=")
            .And.Contain("-p:ApplicationVersion="));
    }

    [TestMethod]
    public void NbgvAndroidTarget_CiVersionInjected_KeepsTheInjectedVersion()
    {
        var output = RunNbgvAndroidTarget("-p:CiVersionInjected=true", "-p:ApplicationDisplayVersion=0.1.172", "-p:ApplicationVersion=100172");

        output.Should().Contain("ANDROID_VERSION=100172|0.1.172");
    }

    [TestMethod]
    public void NbgvAndroidTarget_NoInjection_LetsNbgvDeriveTheVersion()
    {
        var output = RunNbgvAndroidTarget();

        output.Should().Contain($"ANDROID_VERSION={(1 << 16) + 172}|0.1.172-dev");
    }

    private static string[] WindowsProd => ["-p:TargetPlatformIdentifier=windows", "-p:AppChannel=Prod"];

    private static string[] ProdCi(params string[] properties) => ["-p:ContinuousIntegrationBuild=true", "-p:AppChannel=Prod", .. properties];

    // Through the environment, as CI does it: a publisher's commas would split a -p: switch.
    private static Dictionary<string, string> StoreIdentity(string name, string publisher, string displayName) => new()
    {
        ["WINDOWS_STORE_IDENTITY_NAME"] = name,
        ["WINDOWS_STORE_PUBLISHER"] = publisher,
        ["WINDOWS_STORE_PUBLISHER_DISPLAY_NAME"] = displayName,
    };

    private static RunResult RunTarget(string target, string[] properties, Dictionary<string, string>? environment = null)
    {
        var root = FindRepoRoot();
        var directory = Directory.CreateTempSubdirectory("prod-build-guard-");
        try
        {
            var manifest = Path.Combine(directory.FullName, "Package.appxmanifest");
            File.WriteAllText(manifest, $"""
                <Package xmlns="{_foundation}">
                  <Identity Name="Original" Publisher="CN=Original" Version="0.0.0.0" />
                  <Properties><PublisherDisplayName>Original Ltd.</PublisherDisplayName></Properties>
                </Package>
                """);

            // The real props and targets, with the SDKs' evaluation-time defaults between them.
            var project = Path.Combine(directory.FullName, "guard.proj");
            File.WriteAllText(project, $"""
                <Project>
                  <Import Project="{Path.Combine(root, "src", "Directory.Build.props")}" />
                  <PropertyGroup>
                    <ApplicationVersion Condition="'$(ApplicationVersion)' == ''">1</ApplicationVersion>
                    <ApplicationDisplayVersion Condition="'$(ApplicationDisplayVersion)' == ''">1.0.0</ApplicationDisplayVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <AppxManifest Include="Package.appxmanifest" />
                  </ItemGroup>
                  <Import Project="{Path.Combine(root, "src", "Directory.Build.targets")}" />
                </Project>
                """);

            ProcessStartInfo startInfo = new("dotnet")
            {
                WorkingDirectory = directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in new[] { "msbuild", project, "-nologo", "-nodeReuse:false", $"-t:{target}" }.Concat(properties))
            {
                startInfo.ArgumentList.Add(argument);
            }
            foreach (var (key, value) in environment ?? [])
            {
                startInfo.Environment[key] = value;
            }

            using var process = Process.Start(startInfo)!;
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var stamped = XDocument.Load(manifest).Root!;
            var identity = stamped.Element(_foundation + "Identity")!;
            return new(
                process.ExitCode,
                output + error.Result,
                ((string)identity.Attribute("Name")!, (string)identity.Attribute("Publisher")!, (string)stamped.Descendants(_foundation + "PublisherDisplayName").Single()));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    // Runs the real NBGV targets (only GetBuildVersion is stubbed) up to Android's
    // _GetAndroidPackageName, and returns the version the Android SDK would then read.
    private static string RunNbgvAndroidTarget(params string[] properties)
    {
        var root = FindRepoRoot();
        var directory = Directory.CreateTempSubdirectory("nbgv-android-");
        try
        {
            var project = Path.Combine(directory.FullName, "android.proj");
            File.WriteAllText(project, $"""
                <Project>
                  <Import Project="{Path.Combine(root, "src", "Directory.Build.props")}" />
                  <PropertyGroup>
                    <TargetPlatformIdentifier>android</TargetPlatformIdentifier>
                    <ApplicationVersion Condition="'$(ApplicationVersion)' == ''">1</ApplicationVersion>
                    <ApplicationDisplayVersion Condition="'$(ApplicationDisplayVersion)' == ''">1.0.0</ApplicationDisplayVersion>
                    <BuildVersion>0.1.172.0</BuildVersion>
                    <BuildNumber>172</BuildNumber>
                    <Version>0.1.172-dev</Version>
                  </PropertyGroup>
                  <Import Project="{NbgvTargets()}" />
                  <Target Name="GetBuildVersion" />
                  <Target Name="_GetAndroidPackageName" />
                  <Target Name="Report" AfterTargets="_GetAndroidPackageName">
                    <Message Importance="high" Text="ANDROID_VERSION=$(ApplicationVersion)|$(ApplicationDisplayVersion)" />
                  </Target>
                </Project>
                """);

            ProcessStartInfo startInfo = new("dotnet")
            {
                WorkingDirectory = directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in new[] { "msbuild", project, "-nologo", "-t:_GetAndroidPackageName" }.Concat(properties))
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)!;
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd() + error.Result;
            process.WaitForExit();

            process.ExitCode.Should().Be(0, output);
            return output;
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string NbgvTargets()
    {
        var root = FindRepoRoot();
        var version = XDocument.Load(Path.Combine(root, "src", "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Single(package => (string?)package.Attribute("Include") == "Nerdbank.GitVersioning")
            .Attribute("Version")!.Value;
        var packages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        return Path.Combine(packages, "nerdbank.gitversioning", version, "build", "Nerdbank.GitVersioning.targets");
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

    [GeneratedRegex(@"dotnet (?:publish|build)\b(?:[^\n]*\\\r?\n)*[^\n]*")]
    private static partial Regex PackagingCommand();

    private sealed record RunResult(int ExitCode, string Output, (string Name, string Publisher, string DisplayName) Identity);
}
