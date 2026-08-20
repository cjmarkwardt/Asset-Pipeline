namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// The generation lifecycle state of a single <see cref="AssetVariant"/>.
/// </summary>
internal enum VariantStatus
{
    /// <summary>
    /// Generation has not started yet.
    /// </summary>
    Pending,

    /// <summary>
    /// Generation is currently in progress.
    /// </summary>
    Generating,

    /// <summary>
    /// A <see cref="MeshVariant"/>'s concept image has finished generating and, because its owning
    /// <see cref="MeshNode.RequireConceptApproval"/> is enabled, is waiting on user approval before mesh
    /// generation continues.
    /// </summary>
    AwaitingApproval,

    /// <summary>
    /// Generation finished successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Generation failed.
    /// </summary>
    Failed,
}
