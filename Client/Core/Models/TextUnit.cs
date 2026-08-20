namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// The unit a <see cref="TextNode"/>'s <see cref="TextNode.MinUnits"/>/<see cref="TextNode.MaxUnits"/>
/// length constraint is measured in.
/// </summary>
internal enum TextUnit
{
    /// <summary>
    /// Length is measured in characters.
    /// </summary>
    Characters,

    /// <summary>
    /// Length is measured in words.
    /// </summary>
    Words,

    /// <summary>
    /// Length is measured in sentences.
    /// </summary>
    Sentences,

    /// <summary>
    /// Length is measured in paragraphs.
    /// </summary>
    Paragraphs,
}
