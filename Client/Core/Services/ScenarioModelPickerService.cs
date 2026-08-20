namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Asks Claude to pick the single best scenario.com generation model for a node, given that node's own
/// aggregated context and the models available to choose from.
/// </summary>
internal interface IScenarioModelPickerService
{
    /// <summary>
    /// Picks the best model for <paramref name="context"/> out of <paramref name="availableModels"/>.
    /// </summary>
    /// <param name="context">The requesting node's own aggregated context text.</param>
    /// <param name="availableModels">The candidate models to pick from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The picked model's id.</returns>
    Task<string> PickModelAsync(string context, IReadOnlyList<ScenarioModel> availableModels, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IScenarioModelPickerService" />
/// <remarks>
/// Shells out to the local <c>claude</c> CLI (Claude Code) rather than calling a hosted API - see
/// <see cref="ClaudeCodeCli"/>.
/// </remarks>
internal sealed class ClaudeScenarioModelPickerService : IScenarioModelPickerService
{
    /// <inheritdoc />
    public async Task<string> PickModelAsync(string context, IReadOnlyList<ScenarioModel> availableModels, CancellationToken cancellationToken = default)
    {
        if (availableModels.Count == 0)
        {
            throw new InvalidOperationException("No scenario.com models are available to pick from. Refresh the model list first.");
        }

        string pickedId = await ClaudeCodeCli.RunAsync(BuildPrompt(context, availableModels), cancellationToken);
        return availableModels.Any(model => model.Id == pickedId)
            ? pickedId
            : throw new InvalidOperationException($"Claude Code CLI picked an unrecognized model id '{pickedId}'.");
    }

    private static string BuildPrompt(string context, IReadOnlyList<ScenarioModel> availableModels)
    {
        StringBuilder builder = new();
        builder.AppendLine("Pick the single best scenario.com image generation model for the context below, out of the available models listed.");
        builder.AppendLine("Respond with only the chosen model's id and nothing else - no explanation, headers, or surrounding commentary.");
        builder.AppendLine();
        builder.AppendLine("Available models (id: name):");
        foreach (ScenarioModel model in availableModels)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"{model.Id}: {model.Name}");
        }

        builder.AppendLine();
        builder.AppendLine("Context:");
        builder.AppendLine(string.IsNullOrWhiteSpace(context) ? "(no context provided)" : context);
        return builder.ToString();
    }
}
