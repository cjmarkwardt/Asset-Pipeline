namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// An <see cref="AssetNode"/> representing a 3D mesh asset to be generated: a concept image is first
/// generated via scenario.com, then a mesh is generated from that concept image via meshy.ai.
/// </summary>
internal sealed record MeshNode : AssetNode
{
    /// <summary>
    /// Gets a value indicating whether each variant should stop after generating its concept image, and wait
    /// for user approval (see <see cref="VariantStatus.AwaitingApproval"/>) before continuing on to mesh
    /// generation.
    /// </summary>
    public bool RequireConceptApproval { get; init; }
}
