namespace AppTemplate.Core.Infrastructure;

public interface IApplication
{
    ApplicationTheme RequestedTheme { get; }

    ResourceDictionary Resources { get; }

    string AppVersion { get; }

    void Exit();
}
