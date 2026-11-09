using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MZikmund.UnoApp.Wizard;

namespace MZikmund.UnoApp.Cli;

/// <summary>
/// Serves the visual wizard and creates apps on its behalf. It can run <c>dotnet new</c> and write files, so it
/// binds to loopback only, checks the Host header against DNS rebinding, and accepts API calls only from a browser
/// holding this session's cookie, which SameSite=Strict keeps away from other sites.
/// </summary>
internal sealed class WizardServer
{
    private readonly TemplateManifest _manifest;
    private readonly TemplateRunner _runner;
    private readonly string _uiRoot;
    private readonly string _outputRoot;
    private readonly string _token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
    private readonly SemaphoreSlim _createGate = new(1, 1);

    public WizardServer(TemplateManifest manifest, TemplateRunner runner, string uiRoot, string outputRoot)
    {
        _manifest = manifest;
        _runner = runner;
        _uiRoot = uiRoot;
        _outputRoot = outputRoot;
    }

    public async Task RunAsync(int port, bool openBrowser, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { ContentRootPath = _uiRoot, WebRootPath = _uiRoot });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, WizardJsonContext.Default));

        var app = builder.Build();
        app.Use(GuardAsync);

        // The bundle uses extensions such as .wasm and .dat that aren't on the default list.
        FileExtensionContentTypeProvider contentTypes = new();
        contentTypes.Mappings[".wasm"] = "application/wasm";
        contentTypes.Mappings[".dat"] = "application/octet-stream";
        contentTypes.Mappings[".blat"] = "application/octet-stream";
        PhysicalFileProvider files = new(_uiRoot);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
        });

        app.MapGet("/favicon.ico", () => Results.NoContent());
        app.MapGet($"/{PreviewSite.ManifestPath}", () => Results.Json(_manifest, WizardJsonContext.Default.TemplateManifest));
        app.MapGet($"/{PreviewSite.InfoPath}", () => Results.Json(new WizardInfo(Bundled.Version, _outputRoot), WizardJsonContext.Default.WizardInfo));
        app.MapPost("/api/create", (HttpRequest request, CancellationToken aborted) => CreateAsync(request, aborted));
        app.MapPost("/api/shutdown", (IHostApplicationLifetime lifetime) =>
        {
            lifetime.StopApplication();
            return Results.Ok();
        });

        await app.StartAsync(cancellationToken);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        var url = $"{address}/?session={_token}";

        Console.WriteLine($"Wizard running at {url}");
        Console.WriteLine("Press Ctrl+C to stop.");
        if (openBrowser)
        {
            OpenBrowser(url);
        }

        await app.WaitForShutdownAsync(cancellationToken);
    }

    private async Task GuardAsync(HttpContext context, RequestDelegate next)
    {
        var host = context.Request.Host.Host;
        if (host is not ("127.0.0.1" or "localhost"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Another localhost port counts as same-site, so SameSite alone wouldn't stop a page there calling the API.
        if (context.Request.Headers.Origin is { Count: > 0 } origin
            && !string.Equals(origin.ToString(), $"{context.Request.Scheme}://{context.Request.Host}", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Cookies aren't scoped by port, so a second wizard must not overwrite this one's session.
        var cookieName = $"mzuno-session-{context.Request.Host.Port}";
        if (context.Request.Query.TryGetValue("session", out var session) && Matches(session.ToString()))
        {
            context.Response.Cookies.Append(cookieName, _token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Path = "/" });
            context.Response.Redirect("/");
            return;
        }

        var isApi = context.Request.Path.StartsWithSegments("/api");
        if (isApi && !(context.Request.Cookies.TryGetValue(cookieName, out var cookie) && Matches(cookie)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private bool Matches(string candidate) =>
        CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(candidate), System.Text.Encoding.UTF8.GetBytes(_token));

    private async Task<IResult> CreateAsync(HttpRequest httpRequest, CancellationToken aborted)
    {
        var request = await httpRequest.ReadFromJsonAsync(WizardJsonContext.Default.CreateRequest, aborted);
        if (request is null)
        {
            return Results.BadRequest();
        }

        // Rebuilt and revalidated here: the UI validates for friendliness, this is what actually protects the disk.
        WizardSelection selection = new(_manifest) { Name = request.Name };
        foreach (var platform in _manifest.Platforms)
        {
            selection.SetPlatform(platform.Id, request.Platforms.Contains(platform.Id));
        }

        foreach (var parameter in request.Parameters.Where(p => _manifest.Parameters.Any(known => known.Id == p.Key)))
        {
            selection.SetParameter(parameter.Key, parameter.Value);
        }

        try
        {
            selection.SetFeatures(request.Features);
        }
        catch (ArgumentException exception)
        {
            return Results.Json(new CreateResponse(false, exception.Message, null), WizardJsonContext.Default.CreateResponse);
        }

        var problems = selection.Validate();
        if (problems.Count > 0)
        {
            return Results.Json(new CreateResponse(false, string.Join(' ', problems), null), WizardJsonContext.Default.CreateResponse);
        }

        var root = string.IsNullOrWhiteSpace(request.OutputDirectory) ? _outputRoot : request.OutputDirectory;
        selection.OutputDirectory = Path.Combine(root, request.Name);

        await _createGate.WaitAsync(aborted);
        try
        {
            var created = await AppCreator.CreateAsync(selection, _runner, request.InitializeGit);
            var message = created.Warning is null ? $"Created {request.Name}." : $"Created {request.Name}. {created.Warning}";
            return Results.Json(new CreateResponse(true, message, created.Path), WizardJsonContext.Default.CreateResponse);
        }
        catch (CliException exception)
        {
            return Results.Json(new CreateResponse(false, exception.Message, null), WizardJsonContext.Default.CreateResponse);
        }
        finally
        {
            _createGate.Release();
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.WriteLine("Couldn't open a browser. Open the address above yourself.");
        }
    }
}
