namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// Distinguishes how a <see cref="ReferenceFile"/>'s <see cref="ReferenceFile.Path"/> should be resolved.
/// </summary>
internal enum ReferenceFileSource
{
    /// <summary>
    /// The file was uploaded and stored inside the project's data folder; its path is relative to that
    /// folder.
    /// </summary>
    Stored,

    /// <summary>
    /// The file already exists somewhere in the project; its path is relative to the project's root folder.
    /// </summary>
    ProjectPath,
}
