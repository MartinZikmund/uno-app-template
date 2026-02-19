namespace AppTemplate.Shell;

public interface IWindowShellProvider
{
    IWindowShell? Shell { get; }

    Window? Window { get; }
}
