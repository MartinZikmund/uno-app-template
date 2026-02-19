using AppTemplate.Services;
using AppTemplate.Services.Dialogs;
using AppTemplate.Services.Localization;
using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using AppTemplate.Shell;
using AppTemplate.ViewModels;
using Microsoft.Extensions.Localization;
using MZikmund.Toolkit.WinUI.Services;
using Uno.Resizetizer;

namespace AppTemplate;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(ConfigureLogging, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                })
                .UseHttp((context, services) =>
                {
#if DEBUG
                    // DelegatingHandler will be automatically injected
                    services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
                })
                .ConfigureServices(RegisterServices)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        // Initialize the static Localizer
        var stringLocalizer = Host.Services.GetRequiredService<IStringLocalizer>();
        Localizer.Initialize(stringLocalizer);

        // Create WindowShell as the root content
        var shell = new WindowShell(Host.Services, MainWindow);
        MainWindow.Content = shell;

        // Navigate to the main page
        var navigationService = shell.ServiceProvider.GetRequiredService<INavigationService>();
        navigationService.Navigate<MainViewModel>();

        // Ensure the current window is active
        MainWindow.Activate();
    }

    private void RegisterServices(HostBuilderContext context, IServiceCollection services)
    {
        // Singleton (app-wide)
        services.AddSingleton<IPreferences, Preferences>();
        services.AddSingleton<IAppPreferences, AppPreferences>();

        // Scoped (per window)
        services.AddScoped<IWindowShellProvider, WindowShellProvider>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IThemeManager, ThemeManager>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<WindowShellViewModel>();

        // Transient (new per request)
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logBuilder)
    {
        // Configure log levels for different categories of logging
        logBuilder
            .SetMinimumLevel(
                context.HostingEnvironment.IsDevelopment() ?
                    LogLevel.Information :
                    LogLevel.Warning)

            // Default filters for core Uno Platform namespaces
            .CoreLogLevel(LogLevel.Warning);

        // Uno Platform namespace filter groups
        // Uncomment individual methods to see more detailed logging
        //// Generic Xaml events
        //logBuilder.XamlLogLevel(LogLevel.Debug);
        //// Layout specific messages
        //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
        //// Storage messages
        //logBuilder.StorageLogLevel(LogLevel.Debug);
        //// Binding related messages
        //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
        //// Binder memory references tracking
        //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
        //// DevServer and HotReload related
        //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
        //// Debug JS interop
        //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);
    }
}
