using AwesomeAssertions;
using MZikmund.UnoApp.Cli;
using MZikmund.UnoApp.Wizard;
using Spectre.Console.Testing;

namespace Template.GenerationTests;

[TestClass]
public class InteractiveWizardTests
{
    private static TemplateManifest Manifest() => Bundled.Manifest();

    private static TestConsole NewConsole()
    {
        TestConsole console = new();
        console.Profile.Capabilities.Interactive = true;
        console.Profile.Width = 200;
        return console;
    }

    [TestMethod]
    public void Run_AcceptingEveryPrompt_KeepsDefaults()
    {
        var manifest = Manifest();
        WizardSelection selection = new(manifest);
        var console = NewConsole();
        console.Input.PushTextWithEnter("Contoso");
        foreach (var _ in manifest.Parameters)
        {
            console.Input.PushKey(ConsoleKey.Enter);
        }

        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("y");

        var confirmed = InteractiveWizard.Run(console, manifest, selection, askFeatures: true, askPlatforms: true);

        confirmed.Should().BeTrue();
        selection.Name.Should().Be("Contoso");
        selection.ToCommandLine().Should().Be("dotnet new mzuno-app -n Contoso");
    }

    [TestMethod]
    public void Run_TogglingAFeature_SwitchesItOff()
    {
        var manifest = Manifest();
        WizardSelection selection = new(manifest) { Name = "Contoso" };
        var console = NewConsole();
        foreach (var _ in manifest.Parameters)
        {
            console.Input.PushKey(ConsoleKey.Enter);
        }

        // The highlight starts on the first feature of the first group.
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("y");

        InteractiveWizard.Run(console, manifest, selection, askFeatures: true, askPlatforms: false);

        selection.IsEnabled(manifest.Features.First().Id).Should().BeFalse();
    }

    [TestMethod]
    public void Run_BadNameThenGoodName_ExplainsAndRetries()
    {
        var manifest = Manifest();
        WizardSelection selection = new(manifest);
        var console = NewConsole();
        console.Input.PushTextWithEnter("1 bad name");
        console.Input.PushTextWithEnter("Contoso");
        foreach (var _ in manifest.Parameters)
        {
            console.Input.PushKey(ConsoleKey.Enter);
        }

        console.Input.PushTextWithEnter("n");

        var confirmed = InteractiveWizard.Run(console, manifest, selection, askFeatures: false, askPlatforms: false);

        confirmed.Should().BeFalse();
        selection.Name.Should().Be("Contoso");
        console.Output.Should().Contain("must start with a letter");
    }

    [TestMethod]
    public void Run_ValuesAlreadyPassed_AreNotAskedAgain()
    {
        var manifest = Manifest();
        WizardSelection selection = new(manifest) { Name = "Contoso" };
        foreach (var parameter in manifest.Parameters)
        {
            selection.SetParameter(parameter.Id, parameter.Id == "appId" ? "com.contoso.app" : parameter.Id == "githubRepo" ? "contoso/app" : "Contoso");
        }

        var console = NewConsole();
        console.Input.PushTextWithEnter("y");

        InteractiveWizard.Run(console, manifest, selection, askFeatures: false, askPlatforms: false).Should().BeTrue();

        console.Output.Should().NotContain("Display name:");
    }
}
