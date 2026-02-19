using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using Windows.Foundation.Metadata;

namespace AppTemplate.Shell;

public sealed partial class WindowShell : Page, IWindowShell
{
    private readonly IServiceScope _windowScope;
    private readonly Window _associatedWindow;

    public WindowShell(IServiceProvider serviceProvider, Window associatedWindow)
    {
        InitializeComponent();

        _windowScope = serviceProvider.CreateScope();
        _associatedWindow = associatedWindow;

        var windowShellProvider = (WindowShellProvider)ServiceProvider.GetRequiredService<IWindowShellProvider>();
        windowShellProvider.SetShell(this, associatedWindow);

        var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
        navigationService.Initialize(InnerFrame);
        navigationService.RegisterViewsFromAssembly(typeof(App).Assembly);

        var settings = ServiceProvider.GetRequiredService<IAppPreferences>();
        var themeService = ServiceProvider.GetRequiredService<IThemeManager>();
        themeService.SetTheme(settings.Theme);

        ViewModel = ServiceProvider.GetRequiredService<WindowShellViewModel>();

        CustomizeWindow();
    }

    public IServiceProvider ServiceProvider => _windowScope.ServiceProvider;

    public WindowShellViewModel ViewModel { get; }

    public Frame RootFrame => InnerFrame;

    public bool HasCustomTitleBar { get; private set; }

    private void CustomizeWindow()
    {
        if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "ExtendsContentIntoTitleBar"))
        {
#if !HAS_UNO
            _associatedWindow.ExtendsContentIntoTitleBar = true;
            _associatedWindow.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            _associatedWindow.SetTitleBar(DraggableTitleBar);
            HasCustomTitleBar = true;
#endif
        }

        if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "SystemBackdrop"))
        {
            _associatedWindow.SystemBackdrop = new MicaBackdrop();
            Background = null;
        }
    }
}
