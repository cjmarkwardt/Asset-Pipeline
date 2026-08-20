namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// Identifies a project opened in the client, keyed by its root folder on disk.
/// </summary>
internal sealed record ProjectInfo
{
    /// <summary>
    /// The name of the folder, inside a project's root folder, that holds its asset pipeline data. A
    /// folder is only a valid project if it contains a folder with this name.
    /// </summary>
    public const string DataFolderName = ".astproj";

    /// <summary>
    /// Gets the full, normalized path to the project's root folder.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets the display name of the project, derived from the root folder's name.
    /// </summary>
    public string Name =>
        Path.GetFileName(FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) is { Length: > 0 } name
            ? name
            : FullPath;
}
