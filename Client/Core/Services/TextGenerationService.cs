namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Generates a single text asset variant from a <see cref="TextGenerationRequest"/>.
/// </summary>
internal interface ITextGenerationService
{
    /// <summary>
    /// Generates one piece of text satisfying <paramref name="request"/>.
    /// </summary>
    Task<string> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// The context and length/format constraints a single text asset variant must satisfy.
/// </summary>
internal sealed record TextGenerationRequest
{
    /// <summary>
    /// Gets the combined context text (this node's own context plus everything gathered from its ancestor
    /// and linked-node chains) describing what should be generated.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Gets the unit <see cref="MinUnits"/>/<see cref="MaxUnits"/> are measured in.
    /// </summary>
    public required TextUnit Unit { get; init; }

    /// <summary>
    /// Gets the minimum length the generated text must meet.
    /// </summary>
    public required int MinUnits { get; init; }

    /// <summary>
    /// Gets the maximum length the generated text must not exceed.
    /// </summary>
    public required int MaxUnits { get; init; }

    /// <summary>
    /// Gets a value indicating whether the generated text must be valid JSON conforming to
    /// <see cref="JsonSchema"/>.
    /// </summary>
    public bool IsJsonFormatted { get; init; }

    /// <summary>
    /// Gets the JSON schema the generated text must conform to when <see cref="IsJsonFormatted"/> is enabled.
    /// </summary>
    public string? JsonSchema { get; init; }
}

/// <inheritdoc cref="ITextGenerationService" />
/// <remarks>
/// Shells out to the local <c>claude</c> CLI (Claude Code) rather than calling a hosted API, so it relies on
/// that CLI already being installed, on <c>PATH</c>, and authenticated in the environment the client runs in.
/// </remarks>
internal sealed class ClaudeCodeTextGenerationService : ITextGenerationService
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken = default) =>
        ClaudeCodeCli.RunAsync(BuildPrompt(request), cancellationToken);

    private static string BuildPrompt(TextGenerationRequest request)
    {
        StringBuilder builder = new();
        builder.AppendLine("Generate a single text asset variant using only the context below.");
        builder.AppendLine("Respond with the generated text itself and nothing else - no explanation, headers, or surrounding commentary.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Length constraint: between {request.MinUnits} and {request.MaxUnits} {request.Unit.ToString().ToLowerInvariant()}.");

        if (request.IsJsonFormatted)
        {
            builder.AppendLine("Respond with strictly valid JSON only (no markdown code fences) that conforms exactly to this JSON schema:");
            builder.AppendLine(request.JsonSchema);
        }

        builder.AppendLine();
        builder.AppendLine("Context:");
        builder.AppendLine(request.Context);
        return builder.ToString();
    }
}
