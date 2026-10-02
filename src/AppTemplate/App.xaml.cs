using AppTemplate.Core.Infrastructure;
using AppTemplate.Core.Services;
using AppTemplate.Core.ViewModels;
using AppTemplate.Infrastructure;
using AppTemplate.Services.Dialogs;
using AppTemplate.Services.Logging;
using AppTemplate.Services.Navigation;
using AppTemplate.Services.Rating;
using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using Serilog;
using Serilog.Extensions.Logging;
using Uno.Resizetizer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AppTemplate;

public partial class App : Application, IApplication
{
    private readonly string _logFolder;
    private readonly Serilog.Core.Logger _fileLogger;
    private readonly ILogger _bootstrapLogger;

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services => Host!.Services;

    public string AppVersion
    {
        get
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string? WorktreeName =>
        string.IsNullOrEmpty(AppEnvironment.WorktreeName) ? null : AppEnvironment.WorktreeName;

    public App()
    {
        this.InitializeComponent();

        // Built here, not during host building: the handlers below can fire before the host exists.
        // The host gets this same logger, so there's a single handle on the file.
        _logFolder = ResolveLogFolder();
        _fileLogger = CreateFileLogger(_logFolder);
        _bootstrapLogger = new SerilogLoggerFactory(_fileLogger).CreateLogger<App>();
        _bootstrapLogger.LogInformation("Starting, logging to {LogFolder}", _logFolder);

        RegisterGlobalExceptionHandlers();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    /// <summary>The host's pipeline once it's up, the bootstrap file logger until then.</summary>
    private ILogger Log
    {
        get
        {
            try
            {
                return Host?.Services.GetService<ILogger<App>>() ?? _bootstrapLogger;
            }
            catch (ObjectDisposedException)
            {
                return _bootstrapLogger;
            }
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await LaunchAsync(args);
        }
        catch (Exception ex)
        {
            Log.LogCritical(ex, "Application startup failed");

            // A half-started app is worse than a crash, so let it surface.
            throw;
        }
    }

    private async Task LaunchAsync(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
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
        IoC.SetProvider(Host.Services);

        // Run app lifecycle updates
        var appPreferences = Host.Services.GetRequiredService<IAppPreferences>();
        var appUpdater = Host.Services.GetRequiredService<IAppUpdater>();
        await appUpdater.EnsureAppUpToDateAsync();
        appPreferences.LaunchCount++;

        // Create WindowShell as root content
        if (MainWindow.Content is not WindowShell)
        {
            var shell = new WindowShell(Host.Services, MainWindow);
            MainWindow.Content = shell;
        }

        MainWindow.Activate();
    }

    // Records crashes so a user-reported failure has a trail. Nothing is marked handled:
    // the goal is diagnosis, not hiding faults.
    private void RegisterGlobalExceptionHandlers()
    {
        UnhandledException += (_, e) =>
            Log.LogCritical(e.Exception, "Unhandled UI exception: {Message}", e.Message);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.LogCritical(
                e.ExceptionObject as Exception,
                "Unhandled exception (terminating: {IsTerminating})",
                e.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, e) =>
            Log.LogError(e.Exception, "Unobserved task exception");

#if __ANDROID__
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            Log.LogCritical(e.Exception, "Unhandled Android exception");
#endif
    }

    private static string ResolveLogFolder()
    {
        string? appData = null;
        try
        {
            appData = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception)
        {
            // No app data folder on this head; fall back below.
        }

        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Path.Combine(Path.GetTempPath(), "AppTemplate");
        }

        var folder = LogPaths.GetFolder(appData);
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception)
        {
            // Serilog creates it on first write and reports failures through SelfLog - never block startup.
        }

        return folder;
    }

    private static Serilog.Core.Logger CreateFileLogger(string logFolder) =>
        new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(
                LogPaths.GetFilePath(logFolder),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: LogPaths.FileSizeLimitBytes,
                retainedFileCountLimit: LogPaths.RetainedFileCountLimit)
            .CreateLogger();

    private void RegisterServices(HostBuilderContext context, IServiceCollection services)
    {
        // Singleton services
        services.AddSingleton<IApplication>(sp => Current);
        services.AddSingleton<MZikmund.Toolkit.WinUI.Services.IPreferences, Preferences>();
        services.AddSingleton<IAppPreferences, AppPreferences>();
        services.AddSingleton<IDisplayRequestManager, DisplayRequestManager>();
        services.AddSingleton<IAppUpdater, Infrastructure.AppUpdater>();
        services.AddSingleton<ILogFolderProvider>(new LogFolderProvider(_logFolder));
        services.AddSingleton<IFolderLauncher, FolderLauncher>();
        services.AddScoped<IAppRatingService, AppRatingService>();

        // Per-window scoped services
        services.AddScoped<IThemeManager, ThemeManager>();
        services.AddScoped<WindowShellProvider>();
        services.AddScoped<IWindowShellProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
        services.AddScoped<IXamlRootProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
        services.AddScoped<IFrameProvider, FrameProvider>();
        services.AddScoped<IDialogCoordinator, DialogCoordinator>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<IConfirmationDialogService, ConfirmationDialogService>();
        services.AddScoped<IErrorDialogService, ErrorDialogService>();
        services.AddScoped<ILauncherService, LauncherService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<INavigationService>(sp =>
        {
            var service = new NavigationService(sp.GetRequiredService<IFrameProvider>());
            service.RegisterView(typeof(Views.MainView), typeof(MainViewModel));
            service.RegisterView(typeof(Views.SettingsView), typeof(SettingsViewModel));
            return service;
        });

        // Scoped ViewModels
        services.AddScoped<WindowShellViewModel>();

        // Transient ViewModels (new instance per navigation)
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logBuilder)
    {
        logBuilder
            .SetMinimumLevel(
                context.HostingEnvironment.IsDevelopment() ?
                    LogLevel.Information :
                    LogLevel.Warning)
            .CoreLogLevel(LogLevel.Warning)
            .AddSerilog(_fileLogger, dispose: false);
    }
}
