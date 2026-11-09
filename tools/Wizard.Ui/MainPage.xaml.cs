using System.ComponentModel;
using MZikmund.UnoApp.WizardUi.Models;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.UnoApp.WizardUi;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        // The page and the API share an origin, locally and in a hosted preview, so relative URLs are all the client needs.
        var origin = Uno.Foundation.WebAssemblyRuntime.InvokeJS("location.origin");
        HttpClient http = new() { BaseAddress = new Uri(origin.TrimEnd('/') + "/") };
        ViewModel = new WizardViewModel(new WizardApi(http))
        {
            CopyToClipboard = text =>
            {
                DataPackage package = new();
                package.SetText(text);
                Clipboard.SetContent(package);
            },
        };
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public WizardViewModel ViewModel { get; }

    public static Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Hide(bool value) => Show(!value);

    public static bool HasText(string? value) => !string.IsNullOrEmpty(value);

    public static Visibility HasTextVisibility(string? value) => Show(HasText(value));

    public static string NameOrPlaceholder(string name) => name.Length > 0 ? name : "Not set yet";

    public static string VersionText(string version) => version.Length > 0 ? $"Template version {version}" : "";

    private void OnStepSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is StepItem step)
        {
            ViewModel.SelectedStep = step;
        }
    }

    // Back and Next move the step from the view model; the pane follows.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WizardViewModel.IsCreated) or nameof(WizardViewModel.CreateError))
        {
            DispatcherQueue.TryEnqueue(() => (ViewModel.IsCreated ? SuccessBar : ErrorBar).StartBringIntoView());
        }

        if (e.PropertyName == nameof(WizardViewModel.SelectedStep) && !ReferenceEquals(Navigation.SelectedItem, ViewModel.SelectedStep))
        {
            Navigation.SelectedItem = ViewModel.SelectedStep;
        }
    }
}
