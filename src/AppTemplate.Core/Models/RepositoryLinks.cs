namespace AppTemplate.Core.Models;

/// <summary>
/// Links into the app's GitHub repository, built from the configured URL.
/// Nothing is available when the URL is empty or not an http(s) address.
/// </summary>
public sealed class RepositoryLinks
{
    public RepositoryLinks(string? repositoryUrl)
    {
        var trimmed = repositoryUrl?.Trim().TrimEnd('/');
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            RepositoryUri = uri;
            ReportProblemUri = new Uri($"{trimmed}/issues/new");
        }
    }

    public bool IsAvailable => RepositoryUri is not null;

    public Uri? RepositoryUri { get; }

    public Uri? ReportProblemUri { get; }
}
