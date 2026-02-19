namespace AppTemplate.Shell;

public class WindowShellProvider : IWindowShellProvider
{
    public IWindowShell? Shell { get; private set; }

    public Window? Window { get; private set; }

    internal void SetShell(IWindowShell shell, Window window)
    {
        Shell = shell;
        Window = window;
    }
}
