using System.Net.Http.Json;

namespace MZikmund.UnoApp.WizardUi;

/// <summary>The wizard server's endpoints, behind an interface so the view model can be tested without HTTP.</summary>
public interface IWizardApi
{
    Task<TemplateManifest> GetManifestAsync();

    Task<WizardInfo> GetInfoAsync();

    Task<CreateResponse> CreateAsync(CreateRequest request);
}

public sealed class WizardApi(HttpClient http) : IWizardApi
{
    public async Task<TemplateManifest> GetManifestAsync() =>
        await http.GetFromJsonAsync(PreviewSite.ManifestPath, WizardJsonContext.Default.TemplateManifest)
        ?? throw new InvalidDataException("The server returned an empty manifest.");

    public async Task<WizardInfo> GetInfoAsync() =>
        await http.GetFromJsonAsync(PreviewSite.InfoPath, WizardJsonContext.Default.WizardInfo)
        ?? throw new InvalidDataException("The server returned no info.");

    public async Task<CreateResponse> CreateAsync(CreateRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/create", request, WizardJsonContext.Default.CreateRequest);
        return await response.Content.ReadFromJsonAsync(WizardJsonContext.Default.CreateResponse)
            ?? new CreateResponse(false, $"The server answered {(int)response.StatusCode} with no details.", null);
    }
}
