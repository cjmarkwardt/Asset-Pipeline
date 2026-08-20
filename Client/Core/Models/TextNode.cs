namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// An <see cref="AssetNode"/> representing a text asset to be generated.
/// </summary>
internal sealed record TextNode : AssetNode
{
    /// <summary>
    /// Gets the unit <see cref="MinUnits"/> and <see cref="MaxUnits"/> are measured in.
    /// </summary>
    public TextUnit Unit { get; init; } = TextUnit.Words;

    /// <summary>
    /// Gets the minimum length, measured in <see cref="Unit"/>, generated text must meet.
    /// </summary>
    public int MinUnits { get; init; } = 50;

    /// <summary>
    /// Gets the maximum length, measured in <see cref="Unit"/>, generated text must not exceed.
    /// </summary>
    public int MaxUnits { get; init; } = 200;

    /// <summary>
    /// Gets a value indicating whether generated text must be formatted as JSON matching
    /// <see cref="JsonSchema"/>.
    /// </summary>
    public bool IsJsonFormatted { get; init; }

    /// <summary>
    /// Gets the JSON schema generated text must conform to when <see cref="IsJsonFormatted"/> is enabled.
    /// </summary>
    public string JsonSchema { get; init; } = "";
}
