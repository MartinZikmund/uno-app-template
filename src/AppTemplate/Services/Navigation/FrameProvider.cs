namespace AppTemplate.Services.Navigation;

internal sealed class FrameProvider : IFrameProvider
{
    private readonly IWindowShellProvider _shellProvider;

    public FrameProvider(IWindowShellProvider shellProvider)
    {
        _shellProvider = shellProvider;
    }

    public Frame Frame => _shellProvider.RootFrame;
}
