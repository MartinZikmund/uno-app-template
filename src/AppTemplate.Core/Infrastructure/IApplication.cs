namespace AppTemplate.Core.Infrastructure;

public interface IApplication
{
	ApplicationTheme RequestedTheme { get; }

	ResourceDictionary Resources { get; }

	void Exit();
}
