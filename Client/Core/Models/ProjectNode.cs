namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// The persisted form of a single node in a project's node tree. Every node is either a
/// <see cref="GroupNode"/> or an <see cref="AssetNode"/>, or one of their derived types.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Kind")]
[JsonDerivedType(typeof(RootNode), "Root")]
[JsonDerivedType(typeof(GroupNode), "Group")]
[JsonDerivedType(typeof(TextNode), "Text")]
[JsonDerivedType(typeof(ImageNode), "Image")]
[JsonDerivedType(typeof(MeshNode), "Mesh")]
internal abstract record ProjectNode
{
    /// <summary>
    /// Gets the node's stable identifier, unique within its project.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the node's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the free-form context text describing the concept this node represents, referenced when
    /// generating assets nested under it (for a <see cref="GroupNode"/>) or when generating this node's own
    /// asset variants (for an <see cref="AssetNode"/>).
    /// </summary>
    public string Context { get; init; } = "";

    /// <summary>
    /// Gets the reference files supporting this node's <see cref="Context"/>.
    /// </summary>
    public List<ReferenceFile> ReferenceFiles { get; init; } = [];

    /// <summary>
    /// Gets the stable identifiers of the nodes this node links to outside its own hierarchy. A linked
    /// node's own <see cref="Context"/>, <see cref="ReferenceFiles"/>, and full ancestor chain are included
    /// when generating this node's (or a descendant asset node's) variants.
    /// </summary>
    public List<string> LinkedNodeIds { get; init; } = [];

    /// <summary>
    /// Gets the id of the scenario.com model used to generate images (and, for a mesh node, concept
    /// images) at and below this node, or an empty string to inherit it from this node's parent chain.
    /// </summary>
    public string ScenarioModelId { get; init; } = "";
}
