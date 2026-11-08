using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new MZikmund.UnoApp.WizardUi.App())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
