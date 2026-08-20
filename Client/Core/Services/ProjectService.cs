namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Manages which project folders are known to the client: recently opened, currently open, and the last
/// parent folder browsed into when picking a new one.
/// </summary>
internal interface IProjectService
{
    /// <summary>
    /// Gets the recently opened projects that are still valid projects on disk, most recent first.
    /// </summary>
    Task<IReadOnlyList<ProjectInfo>> GetRecentProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the project rooted at <paramref name="folderPath"/> and records it as the most recently
    /// opened project.
    /// </summary>
    /// <param name="folderPath">The full path to the candidate project's root folder.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The opened project, or <see langword="null"/> if <paramref name="folderPath"/> does not contain a
    /// <see cref="ProjectInfo.DataFolderName"/> folder and so is not a valid project.
    /// </returns>
    Task<ProjectInfo?> OpenAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new project rooted at <paramref name="folderPath"/> by adding a
    /// <see cref="ProjectInfo.DataFolderName"/> folder, and records it as the most recently opened project.
    /// </summary>
    /// <param name="folderPath">The full path to the folder to turn into a project.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created project.</returns>
    Task<ProjectInfo> CreateAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a project from the recently opened list, without affecting whether it is currently open.
    /// </summary>
    Task ForgetRecentAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the projects that were open as tabs when the app last closed, filtered to folders that are
    /// still valid projects.
    /// </summary>
    Task<IReadOnlyList<ProjectInfo>> GetOpenProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the exact set of currently open project tabs, so they can be restored on the next launch.
    /// </summary>
    Task SaveOpenProjectsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parent directory the folder picker was last browsed into, or <see langword="null"/> if it
    /// has never been set or no longer exists.
    /// </summary>
    Task<string?> GetLastParentFolderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the parent directory the folder picker was last browsed into.
    /// </summary>
    Task SaveLastParentFolderAsync(string directoryPath, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IProjectService" />
internal sealed class ProjectService(ISettingsService settingsService) : IProjectService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectInfo>> GetRecentProjectsAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        return [.. settings.RecentProjectPaths.Where(IsValidProjectFolder).Select(path => new ProjectInfo { FullPath = path })];
    }

    /// <inheritdoc />
    public async Task<ProjectInfo?> OpenAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(folderPath);
        if (!IsValidProjectFolder(fullPath))
        {
            return null;
        }

        await RecordAsRecentAsync(fullPath, cancellationToken);
        return new ProjectInfo { FullPath = fullPath };
    }

    /// <inheritdoc />
    public async Task<ProjectInfo> CreateAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(folderPath);
        Directory.CreateDirectory(Path.Combine(fullPath, ProjectInfo.DataFolderName));
        await RecordAsRecentAsync(fullPath, cancellationToken);
        return new ProjectInfo { FullPath = fullPath };
    }

    /// <inheritdoc />
    public async Task ForgetRecentAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(folderPath);
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        settings.RecentProjectPaths.RemoveAll(path => string.Equals(Path.GetFullPath(path), fullPath, StringComparison.Ordinal));
        await settingsService.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectInfo>> GetOpenProjectsAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        return [.. settings.OpenProjectPaths.Where(IsValidProjectFolder).Select(path => new ProjectInfo { FullPath = path })];
    }

    /// <inheritdoc />
    public async Task SaveOpenProjectsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        AppSettings updated = settings with { OpenProjectPaths = [.. paths] };
        await settingsService.SaveAsync(updated, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetLastParentFolderAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        return settings.LastParentFolderPath is { } path && Directory.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public async Task SaveLastParentFolderAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        AppSettings updated = settings with { LastParentFolderPath = Path.GetFullPath(directoryPath) };
        await settingsService.SaveAsync(updated, cancellationToken);
    }

    private static bool IsValidProjectFolder(string folderPath) =>
        Directory.Exists(Path.Combine(folderPath, ProjectInfo.DataFolderName));

    private async Task RecordAsRecentAsync(string fullPath, CancellationToken cancellationToken)
    {
        AppSettings settings = await settingsService.LoadAsync(cancellationToken);
        settings.RecentProjectPaths.RemoveAll(path => string.Equals(Path.GetFullPath(path), fullPath, StringComparison.Ordinal));
        settings.RecentProjectPaths.Insert(0, fullPath);
        await settingsService.SaveAsync(settings, cancellationToken);
    }
}
