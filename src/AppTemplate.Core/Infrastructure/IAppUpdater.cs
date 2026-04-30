namespace AppTemplate.Core.Infrastructure;

public interface IAppUpdater
{
	Task EnsureAppUpToDateAsync();
}
