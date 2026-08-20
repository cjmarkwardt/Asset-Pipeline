namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Loads and persists a single project's node tree from/to that project's <see cref="ProjectInfo.DataFolderName"/> folder.
/// </summary>
internal interface IProjectDataStore
{
    /// <summary>
    /// Loads the node tree for the project rooted at <paramref name="projectPath"/>, or a fresh
    /// default tree (a lone root node) if none has been saved yet.
    /// </summary>
    Task<ProjectNode> LoadTreeAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <paramref name="root"/> (and its full nested tree of children) as the node tree for the
    /// project rooted at <paramref name="projectPath"/>.
    /// </summary>
    Task SaveTreeAsync(string projectPath, ProjectNode root, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies <paramref name="sourceFilePath"/> into the project's data folder as a stored reference file.
    /// </summary>
    /// <param name="projectPath">The full path to the project's root folder.</param>
    /// <param name="sourceFilePath">The full path to the file being uploaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The stored file's path, relative to the project's data folder.</returns>
    Task<string> StoreReferenceFileAsync(string projectPath, string sourceFilePath, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IProjectDataStore" />
internal sealed class ProjectDataStore : IProjectDataStore
{
    private const string TreeFileName = "project.json";
    private const string RootNodeId = "root";
    private const string ReferenceFilesFolderName = "references";

    /// <inheritdoc />
    public async Task<ProjectNode> LoadTreeAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string filePath = GetTreeFilePath(projectPath);
        if (!File.Exists(filePath))
        {
            return CreateDefaultRoot();
        }

        try
        {
            await using FileStream stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<ProjectNode>(stream, AppJson.Options, cancellationToken)
                   ?? CreateDefaultRoot();
        }
        catch (JsonException)
        {
            return CreateDefaultRoot();
        }
    }

    /// <inheritdoc />
    public async Task SaveTreeAsync(string projectPath, ProjectNode root, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.Combine(projectPath, ProjectInfo.DataFolderName));
        await using FileStream stream = File.Create(GetTreeFilePath(projectPath));
        await JsonSerializer.SerializeAsync(stream, root, AppJson.Options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> StoreReferenceFileAsync(string projectPath, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        string referencesFolder = Path.Combine(projectPath, ProjectInfo.DataFolderName, ReferenceFilesFolderName);
        Directory.CreateDirectory(referencesFolder);

        string extension = Path.GetExtension(sourceFilePath);
        string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
        string destinationFileName = $"{baseName}{extension}";
        int suffix = 1;
        while (File.Exists(Path.Combine(referencesFolder, destinationFileName)))
        {
            destinationFileName = $"{baseName}-{suffix++}{extension}";
        }

        await using FileStream source = File.OpenRead(sourceFilePath);
        await using FileStream destination = File.Create(Path.Combine(referencesFolder, destinationFileName));
        await source.CopyToAsync(destination, cancellationToken);

        return Path.Combine(ReferenceFilesFolderName, destinationFileName);
    }

    private static string GetTreeFilePath(string projectPath) =>
        Path.Combine(projectPath, ProjectInfo.DataFolderName, TreeFileName);

    private static ProjectNode CreateDefaultRoot() => new RootNode { Id = RootNodeId, Name = "Project" };
}
