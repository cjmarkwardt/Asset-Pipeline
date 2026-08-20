namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// Persisted, user-scoped application settings.
/// </summary>
internal sealed record AppSettings
{
    /// <summary>
    /// Gets the full paths of projects opened recently, most recent first.
    /// </summary>
    public List<string> RecentProjectPaths { get; init; } = [];

    /// <summary>
    /// Gets the full paths of the projects that were open as tabs when the app last closed. Restored as
    /// tabs on the next launch, skipping any that are no longer valid projects.
    /// </summary>
    public List<string> OpenProjectPaths { get; init; } = [];

    /// <summary>
    /// Gets the parent directory the folder picker was last browsed into for "Open Project", or
    /// <see langword="null"/> if it has never been used.
    /// </summary>
    public string? LastParentFolderPath { get; init; }

    /// <summary>
    /// Gets the credentials for the third-party generation APIs the client calls out to.
    /// </summary>
    public ApiSettings Api { get; init; } = new();
}
