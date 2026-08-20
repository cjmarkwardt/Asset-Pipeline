namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// A node representing a specific asset to be generated, as opposed to a <see cref="GroupNode"/>, which
/// only organizes other nodes.
/// </summary>
internal abstract record AssetNode : ProjectNode
{
    /// <summary>
    /// Gets the project-relative path of the folder generated asset variants are written into.
    /// </summary>
    public string OutputPath { get; init; } = "";

    /// <summary>
    /// Gets this node's generated (and in-progress) asset variants.
    /// </summary>
    public List<AssetVariant> Variants { get; init; } = [];
}
