using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new AppTemplate.App())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
