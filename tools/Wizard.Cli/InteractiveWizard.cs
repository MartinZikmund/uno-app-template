using MZikmund.UnoApp.Wizard;
using Spectre.Console;

namespace MZikmund.UnoApp.Cli;

/// <summary>The terminal wizard: asks only for what wasn't already passed on the command line.</summary>
internal static class InteractiveWizard
{
    public static bool Run(IAnsiConsole console, TemplateManifest manifest, WizardSelection selection, bool askFeatures, bool askPlatforms)
    {
        console.Write(new Rule("[bold]New Uno app[/]").LeftJustified());

        if (selection.Name.Length == 0)
        {
            selection.Name = console.Prompt(new TextPrompt<string>("Project [green]name[/]:")
                .Validate(name =>
                {
                    WizardSelection probe = new(manifest) { Name = name };
                    var problem = probe.Validate().FirstOrDefault();
                    return problem is null ? ValidationResult.Success() : ValidationResult.Error($"[red]{Markup.Escape(problem)}[/]");
                }));
        }

        foreach (var parameter in manifest.Parameters.Where(p => selection.GetParameter(p.Id).Length == 0))
        {
            var fallback = parameter.DefaultValue.Length > 0 ? parameter.DefaultValue : DefaultFor(parameter.Id, selection.Name);
            var value = console.Prompt(new TextPrompt<string>($"{Markup.Escape(parameter.DisplayName)}:")
                .DefaultValue(fallback)
                .Validate(text =>
                {
                    WizardSelection probe = new(manifest) { Name = selection.Name };
                    probe.SetParameter(parameter.Id, text);
                    var problem = probe.Validate().FirstOrDefault();
                    return problem is null ? ValidationResult.Success() : ValidationResult.Error($"[red]{Markup.Escape(problem)}[/]");
                }));
            selection.SetParameter(parameter.Id, value == fallback && parameter.DefaultValue.Length == 0 ? "" : value);
        }

        if (askPlatforms)
        {
            MultiSelectionPrompt<ManifestChoice> prompt = new MultiSelectionPrompt<ManifestChoice>()
                .Title("Which [green]platforms[/]?")
                .Required()
                .InstructionsText("[grey](space to toggle, enter to accept)[/]")
                .UseConverter(platform => $"{Markup.Escape(platform.DisplayName)} [grey]{Markup.Escape(platform.Description)}[/]")
                .AddChoices(manifest.Platforms);
            foreach (var platform in manifest.Platforms.Where(p => selection.IsPlatformEnabled(p.Id)))
            {
                prompt.Select(platform);
            }

            var platforms = console.Prompt(prompt);
            foreach (var platform in manifest.Platforms)
            {
                selection.SetPlatform(platform.Id, platforms.Contains(platform));
            }
        }

        if (askFeatures)
        {
            // Flat on purpose: Spectre's group rows show as unticked over ticked children, and space on one toggles them all.
            var groupOf = manifest.Groups.SelectMany(group => group.Features.Select(feature => (feature.Id, group.Title))).ToDictionary();
            MultiSelectionPrompt<ManifestFeature> prompt = new MultiSelectionPrompt<ManifestFeature>()
                .Title("Which [green]features[/]?")
                .PageSize(24)
                .NotRequired()
                .InstructionsText("[grey](space to toggle, enter to accept)[/]")
                .UseConverter(feature => $"{Markup.Escape(feature.DisplayName)} [grey]({Markup.Escape(groupOf[feature.Id])}) {Markup.Escape(feature.Description)}[/]")
                .AddChoices(manifest.Features);
            foreach (var feature in manifest.Features.Where(f => selection.IsEnabled(f.Id)))
            {
                prompt.Select(feature);
            }

            var chosen = console.Prompt(prompt).Select(f => f.Id).ToList();
            foreach (var kept in selection.SetFeatures(chosen))
            {
                var by = string.Join(", ", selection.RequiredBy(kept));
                console.MarkupLineInterpolated($"[yellow]Keeping {kept}[/] because {by} needs it.");
            }
        }

        console.WriteLine();
        console.MarkupLine("[grey]Equivalent command:[/]");
        console.WriteLine(selection.ToCommandLine());
        console.WriteLine();
        return console.Confirm("Create the app?");
    }

    private static string DefaultFor(string parameter, string name) => parameter switch
    {
        "title" => name,
        "appId" => $"com.example.{new string([.. name.Where(char.IsLetterOrDigit)]).ToLowerInvariant()}",
        _ => "",
    };
}
