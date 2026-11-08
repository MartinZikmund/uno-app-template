using System.ComponentModel;

namespace MZikmund.UnoApp.WizardUi.Models;

public enum StepKind
{
    Project,
    Platforms,
    Group,
    Review,
}

public sealed record StepItem(string Id, string Title, string Glyph, StepKind Kind, ManifestGroup? Group = null)
{
    public string AutomationId => $"Step_{Id}";
}

/// <summary>A platform row. The switch reads and writes the shared <see cref="WizardSelection"/>.</summary>
public sealed class PlatformItem(ManifestChoice choice, WizardSelection selection, Action changed)
{
    public string DisplayName => choice.DisplayName;

    public string Description => choice.Description;

    public string AutomationId => $"Platform_{choice.Id}";

    public bool IsOn
    {
        get => selection.IsPlatformEnabled(choice.Id);
        set
        {
            selection.SetPlatform(choice.Id, value);
            changed();
        }
    }
}

/// <summary>A feature row. Turning one off that another needs is refused, and the switch snaps back with a note.</summary>
public sealed class FeatureItem : INotifyPropertyChanged
{
    private readonly ManifestFeature _feature;
    private readonly WizardViewModel _owner;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;

    public FeatureItem(ManifestFeature feature, WizardViewModel owner)
    {
        _feature = feature;
        _owner = owner;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName => _feature.DisplayName;

    public string Description => _feature.Description;

    public string AutomationId => $"Feature_{_feature.Id}";

    public string Note => _owner.NeededByNote(_feature.Id);

    public bool HasNote => Note.Length > 0;

    public bool IsOn
    {
        get => _owner.Selection.IsEnabled(_feature.Id);
        set
        {
            if (_owner.TrySetFeature(_feature.Id, value))
            {
                return;
            }

            // Raised after the switch has finished flipping, otherwise the binding ignores it as an echo.
            if (_context is null)
            {
                Refresh();
            }
            else
            {
                _context.Post(_ => Refresh(), null);
            }
        }
    }

    public void Refresh()
    {
        foreach (var name in new[] { nameof(IsOn), nameof(Note), nameof(HasNote) })
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

/// <summary>A text parameter row (display name, application id, ...).</summary>
public sealed class ParameterItem(ManifestParameter parameter, WizardSelection selection, Action changed, Func<string, string> problem)
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id => parameter.Id;

    public string DisplayName => parameter.DisplayName;

    public string Description => parameter.Description;

    public string Placeholder => parameter.DefaultValue;

    public string AutomationId => $"Parameter_{parameter.Id}";

    public string Problem => problem(parameter.Id);

    public bool HasProblem => Problem.Length > 0;

    public string Value
    {
        get => selection.GetParameter(parameter.Id);
        set
        {
            selection.SetParameter(parameter.Id, value);
            changed();
        }
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Problem)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasProblem)));
    }
}
