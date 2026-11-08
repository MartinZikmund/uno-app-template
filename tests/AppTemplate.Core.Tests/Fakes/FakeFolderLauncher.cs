using AppTemplate.Services;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeFolderLauncher : IFolderLauncher
{
    public bool IsSupported { get; set; } = true;

    public bool Succeeds { get; set; } = true;

    public List<string> OpenedPaths { get; } = [];

    public Task<bool> OpenAsync(string path)
    {
        OpenedPaths.Add(path);
        return Task.FromResult(Succeeds);
    }
}
