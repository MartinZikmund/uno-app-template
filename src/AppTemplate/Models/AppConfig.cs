namespace AppTemplate.Models;

public record AppConfig
{
    public string? Environment { get; init; }

    /// <summary>GitHub repository URL shown under Settings. Leave empty to hide it.</summary>
    public string? RepositoryUrl { get; init; }
}
