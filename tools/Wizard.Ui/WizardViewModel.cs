using MZikmund.UnoApp.WizardUi.Models;

namespace MZikmund.UnoApp.WizardUi;

/// <summary>Wraps <see cref="WizardSelection"/> for the view: steps, rows, the live command and the create call.</summary>
public sealed partial class WizardViewModel : ObservableObject
{
    private readonly IWizardApi _api;
    private TemplateManifest? _manifest;
    private WizardSelection? _selection;
    private List<FeatureItem> _allFeatures = [];
    private Dictionary<string, List<FeatureItem>> _featuresByGroup = [];

    public WizardViewModel(IWizardApi api)
    {
        _api = api;
        IsLoading = true;
        InitializeGit = true;
        OutputDirectory = "";
        Name = "";
        CommandLine = "";
        Summary = "";
        Problems = [];
        NameProblem = "";
        PlatformsText = "";
        FeaturesOnText = "";
        FeaturesOffText = "";
        NextSteps = "";
        CopyLabel = "Copy";
    }

    /// <summary>Set by the view; the view model never touches the clipboard itself.</summary>
    public Action<string>? CopyToClipboard { get; set; }

    public WizardSelection Selection => _selection ?? throw new InvalidOperationException("The manifest isn't loaded yet.");

    public string Version { get; private set; } = "";

    /// <summary>A hosted preview: every choice works and the command is real, but nothing can create the app.</summary>
    public bool IsPreview { get; private set; }

    public string ProjectIntro => IsPreview ? "Name your app." : "Name your app and decide where it goes.";

    public string ReviewIntro => IsPreview ? "Check your choices, then copy the command." : "Check your choices, then create the app.";

    public IReadOnlyList<StepItem> Steps { get; private set; } = [];

    public IReadOnlyList<ParameterItem> Parameters { get; private set; } = [];

    public IReadOnlyList<PlatformItem> PlatformItems { get; private set; } = [];

    public IReadOnlyList<FeatureItem> CurrentFeatures =>
        SelectedStep?.Group is { } group && _featuresByGroup.TryGetValue(group.Id, out var items) ? items : [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? LoadError { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string OutputDirectory { get; set; }

    [ObservableProperty]
    public partial bool InitializeGit { get; set; }

    [ObservableProperty]
    public partial StepItem? SelectedStep { get; set; }

    [ObservableProperty]
    public partial string CommandLine { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; }

    [ObservableProperty]
    public partial int PlatformCount { get; set; }

    [ObservableProperty]
    public partial int FeatureCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Problems { get; set; }

    [ObservableProperty]
    public partial string NameProblem { get; set; }

    [ObservableProperty]
    public partial string PlatformsText { get; set; }

    [ObservableProperty]
    public partial string FeaturesOnText { get; set; }

    [ObservableProperty]
    public partial string FeaturesOffText { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsCreated { get; set; }

    [ObservableProperty]
    public partial string? CreatedMessage { get; set; }

    [ObservableProperty]
    public partial string NextSteps { get; set; }

    [ObservableProperty]
    public partial string? CreateError { get; set; }

    [ObservableProperty]
    public partial string CopyLabel { get; set; }

    public bool IsLoaded => !IsLoading && LoadError is null;

    public bool IsProjectStep => SelectedStep?.Kind == StepKind.Project;

    public bool IsPlatformsStep => SelectedStep?.Kind == StepKind.Platforms;

    public bool IsGroupStep => SelectedStep?.Kind == StepKind.Group;

    public bool IsReviewStep => SelectedStep?.Kind == StepKind.Review;

    public bool IsValid => Problems.Count == 0;

    public bool CanGoBack => IndexOfSelected > 0;

    public bool CanGoNext => IndexOfSelected >= 0 && IndexOfSelected < Steps.Count - 1;

    private int IndexOfSelected => SelectedStep is null ? -1 : IndexOf(SelectedStep);

    public async Task LoadAsync()
    {
        IsLoading = true;
        LoadError = null;
        try
        {
            var manifest = await _api.GetManifestAsync();
            var info = await _api.GetInfoAsync();
            Initialize(manifest, info);
        }
        catch (Exception exception)
        {
            LoadError = $"Couldn't load the template from the wizard server: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsLoaded));
        }
    }

    public void Initialize(TemplateManifest manifest, WizardInfo info)
    {
        _manifest = manifest;
        _selection = new WizardSelection(manifest);
        Version = info.Version;
        OutputDirectory = info.OutputRoot;
        IsPreview = !info.CanCreate;
        OnPropertyChanged(nameof(IsPreview));
        OnPropertyChanged(nameof(ProjectIntro));
        OnPropertyChanged(nameof(ReviewIntro));

        PlatformItems = [.. manifest.Platforms.Select(platform => new PlatformItem(platform, _selection, Refresh))];
        Parameters = [.. manifest.Parameters.Select(parameter => new ParameterItem(parameter, _selection, Refresh, ProblemFor))];

        _featuresByGroup = manifest.Groups.ToDictionary(
            group => group.Id,
            group => group.Features.Select(feature => new FeatureItem(feature, this)).ToList());
        _allFeatures = [.. _featuresByGroup.Values.SelectMany(items => items)];

        List<StepItem> steps =
        [
            new("project", "Project", "", StepKind.Project),
            new("platforms", "Platforms", "", StepKind.Platforms),
            .. manifest.Groups.Select(group => new StepItem(group.Id, group.Title, group.Icon, StepKind.Group, group)),
            new("review", "Review", "", StepKind.Review),
        ];
        Steps = steps;
        SelectedStep = steps[0];
        Refresh();
        OnPropertyChanged(nameof(Steps));
        OnPropertyChanged(nameof(Parameters));
        OnPropertyChanged(nameof(PlatformItems));
    }

    /// <returns><see langword="false"/> when something else needs the feature and it stayed on.</returns>
    public bool TrySetFeature(string feature, bool enabled)
    {
        var accepted = Selection.SetFeature(feature, enabled);
        Refresh();
        return accepted;
    }

    public string NeededByNote(string feature)
    {
        if (_selection is null || _manifest is null)
        {
            return "";
        }

        var names = _selection.RequiredBy(feature)
            .Select(id => _manifest.FindFeature(id)?.DisplayName ?? id)
            .ToList();
        return names.Count == 0 ? "" : $"Needed by {string.Join(", ", names)}";
    }

    partial void OnNameChanged(string value) => Refresh();

    partial void OnOutputDirectoryChanged(string value) => Refresh();

    partial void OnSelectedStepChanged(StepItem? value)
    {
        OnPropertyChanged(nameof(IsProjectStep));
        OnPropertyChanged(nameof(IsPlatformsStep));
        OnPropertyChanged(nameof(IsGroupStep));
        OnPropertyChanged(nameof(IsReviewStep));
        OnPropertyChanged(nameof(CurrentFeatures));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        BackCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value) => CreateCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => SelectedStep = Steps[IndexOfSelected - 1];

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => SelectedStep = Steps[IndexOfSelected + 1];

    [RelayCommand]
    private async Task CopyAsync()
    {
        CopyToClipboard?.Invoke(CommandLine);
        CopyLabel = "Copied";
        await Task.Delay(1500);
        CopyLabel = "Copy";
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync()
    {
        var selection = Selection;
        IsBusy = true;
        IsCreated = false;
        CreatedMessage = null;
        CreateError = null;
        try
        {
            Dictionary<string, string> parameters = [];
            foreach (var parameter in Parameters)
            {
                if (parameter.Value.Length > 0)
                {
                    parameters[parameter.Id] = parameter.Value;
                }
            }

            CreateRequest request = new(Name, OutputDirectory, [.. selection.Platforms], [.. selection.Features], parameters, InitializeGit);
            var response = await _api.CreateAsync(request);
            if (response.Success)
            {
                var path = response.Path ?? "";
                var separator = path.Contains('\\') ? '\\' : '/';
                CreatedMessage = path;
                NextSteps = $"Created in {path}\n\ncd \"{path}\"\ndotnet build \"{path}{separator}src{separator}{Name}{separator}{Name}.csproj\" -f <target framework>";
                IsCreated = true;
            }
            else
            {
                CreateError = response.Message;
            }
        }
        catch (Exception exception)
        {
            CreateError = $"The request failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreate() => !IsPreview && IsValid && !IsBusy;

    private int IndexOf(StepItem step)
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            if (Steps[i] == step)
            {
                return i;
            }
        }

        return -1;
    }

    // Maps Validate()'s messages to the field they're about, so they can sit under it.
    private string ProblemFor(string field)
    {
        if (_selection is null)
        {
            return "";
        }

        if (_selection.GetParameter(field).Length == 0)
        {
            return "";
        }

        var prefix = field switch
        {
            "appId" => "The application id",
            "title" => "The display name",
            "githubRepo" => "The GitHub repository",
            _ => null,
        };
        return _selection.Validate().FirstOrDefault(p => prefix is not null && p.StartsWith(prefix, StringComparison.Ordinal)) ?? "";
    }

    private void Refresh()
    {
        if (_selection is null || _manifest is null)
        {
            return;
        }

        _selection.Name = Name.Trim();
        _selection.OutputDirectory = Name.Trim().Length == 0 || OutputDirectory.Length == 0
            ? ""
            : Combine(OutputDirectory, Name.Trim());

        foreach (var item in _allFeatures)
        {
            item.Refresh();
        }

        foreach (var item in Parameters)
        {
            item.Refresh();
        }

        Problems = _selection.Validate();
        NameProblem = Name.Length == 0
            ? ""
            : _selection.Validate().FirstOrDefault(p => p.StartsWith("The project name", StringComparison.Ordinal) || p.StartsWith("Pick a name", StringComparison.Ordinal)) ?? "";
        CommandLine = _selection.ToCommandLine();
        PlatformCount = _selection.Platforms.Count;
        FeatureCount = _selection.Features.Count;
        Summary = $"{PlatformCount} {(PlatformCount == 1 ? "platform" : "platforms")}, {FeatureCount} {(FeatureCount == 1 ? "feature" : "features")}";

        PlatformsText = ListOrNone(_manifest.Platforms.Where(p => _selection.IsPlatformEnabled(p.Id)).Select(p => p.DisplayName));
        FeaturesOnText = ListOrNone(_manifest.Features.Where(f => _selection.IsEnabled(f.Id)).Select(f => f.DisplayName));
        FeaturesOffText = ListOrNone(_manifest.Features.Where(f => !_selection.IsEnabled(f.Id)).Select(f => f.DisplayName));

        OnPropertyChanged(nameof(IsValid));
        CreateCommand.NotifyCanExecuteChanged();
    }

    private static string ListOrNone(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count == 0 ? "None" : string.Join(", ", list);
    }

    // Plain string work: the browser build has no use for the host's path rules.
    private static string Combine(string root, string name)
    {
        var separator = root.Contains('\\') ? '\\' : '/';
        return root.TrimEnd('\\', '/') + separator + name;
    }
}
