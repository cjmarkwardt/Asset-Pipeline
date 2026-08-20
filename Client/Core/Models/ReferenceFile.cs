namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// A single reference file attached to a <see cref="GroupNode"/>, available to be cited by name from that
/// group's <see cref="GroupNode.Context"/> text.
/// </summary>
internal sealed record ReferenceFile
{
    private readonly HashSet<string> imageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    /// <summary>
    /// Gets the user-assigned name this file is referenced by from context text.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets where this file is stored: either copied into the project's data folder, or located by a path
    /// relative to the project's root folder.
    /// </summary>
    public required ReferenceFileSource Source { get; init; }

    /// <summary>
    /// Gets the file's path, relative to the project's data folder if <see cref="Source"/> is
    /// <see cref="ReferenceFileSource.Stored"/>, or relative to the project's root folder if
    /// <see cref="ReferenceFileSource.ProjectPath"/>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets a value indicating whether this file's extension identifies it as an image, meaning it should be
    /// displayed directly in the group node's content area rather than just listed by name.
    /// </summary>
    public bool IsImage => imageExtensions.Contains(System.IO.Path.GetExtension(Path));
}
