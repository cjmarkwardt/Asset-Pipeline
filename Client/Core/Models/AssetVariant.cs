namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// The persisted form of a single generated (or in-progress) variant of an <see cref="AssetNode"/>'s asset.
/// Every variant is one of <see cref="TextVariant"/>, <see cref="ImageVariant"/>, or
/// <see cref="MeshVariant"/>, matching the kind of its owning <see cref="AssetNode"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Kind")]
[JsonDerivedType(typeof(TextVariant), "Text")]
[JsonDerivedType(typeof(ImageVariant), "Image")]
[JsonDerivedType(typeof(MeshVariant), "Mesh")]
internal abstract record AssetVariant
{
    /// <summary>
    /// Gets the variant's stable identifier, unique within its owning node.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the variant's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the variant's current generation lifecycle state.
    /// </summary>
    public required VariantStatus Status { get; init; }

    /// <summary>
    /// Gets the moment this variant was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the error message from the most recent failed generation attempt, or <see langword="null"/> if
    /// the last attempt did not fail.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
