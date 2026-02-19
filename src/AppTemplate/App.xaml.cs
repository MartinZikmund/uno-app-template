using AppTemplate.Core.Infrastructure;
using AppTemplate.Core.Services;
using AppTemplate.Core.ViewModels;
using AppTemplate.Services.Dialogs;
using AppTemplate.Services.Navigation;
using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using AppTemplate.ViewModels;
using Uno.Resizetizer;

namespace AppTemplate;

public partial class App : Application
{
	public static new App Current => (App)Application.Current;

	public IServiceProvider Services => Host!.Services;

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

		// Create WindowShell as root content
		if (MainWindow.Content is not WindowShell)
		{
			var shell = new WindowShell(Host.Services, MainWindow);
			MainWindow.Content = shell;
		}

		MainWindow.Activate();
	}

	private static void RegisterServices(HostBuilderContext context, IServiceCollection services)
	{
		// Singleton services
		services.AddSingleton<IPreferences, Preferences>();
		services.AddSingleton<IAppPreferences, AppPreferences>();

		// Per-window scoped services
		services.AddScoped<IThemeManager, ThemeManager>();
		services.AddScoped<WindowShellProvider>();
		services.AddScoped<IWindowShellProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
		services.AddScoped<IXamlRootProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
		services.AddScoped<IDialogCoordinator, DialogCoordinator>();
		services.AddScoped<IDialogService, DialogService>();
		services.AddScoped<IConfirmationDialogService, ConfirmationDialogService>();
		services.AddScoped<ILauncherService, LauncherService>();
		services.AddScoped<INavigationService>(sp =>
		{
			var service = new NavigationService(sp.GetRequiredService<IWindowShellProvider>());
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

	private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logBuilder)
	{
		logBuilder
			.SetMinimumLevel(
				context.HostingEnvironment.IsDevelopment() ?
					LogLevel.Information :
					LogLevel.Warning)
			.CoreLogLevel(LogLevel.Warning);
	}
}
