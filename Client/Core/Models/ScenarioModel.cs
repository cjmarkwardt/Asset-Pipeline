namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// A generation model published on scenario.com that can be selected for image generation.
/// </summary>
internal sealed record ScenarioModel
{
    /// <summary>
    /// Gets the model's scenario.com identifier, sent as <c>modelId</c> when submitting a generation job.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the model's human-readable display name.
    /// </summary>
    public required string Name { get; init; }
}
