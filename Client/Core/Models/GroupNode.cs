namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// A node that organizes other nodes underneath it and represents a concept referenced when generating
/// assets nested under it. Holds free-form context text plus any reference files supporting that context.
/// </summary>
internal record GroupNode : ProjectNode
{
    /// <summary>
    /// Gets this node's child nodes.
    /// </summary>
    public List<ProjectNode> Children { get; init; } = [];
}
