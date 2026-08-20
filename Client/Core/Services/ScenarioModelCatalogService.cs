namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Looks up the scenario.com models available for image generation.
/// </summary>
internal interface IScenarioModelCatalogService
{
    /// <summary>
    /// Fetches every public scenario.com model that supports text-to-image generation via the
    /// <c>generate/txt2img</c> endpoint. This excludes standalone third-party foundation models (e.g. GPT
    /// Image, Gemini, Seedream), which scenario.com only exposes through the separate
    /// <c>generate/custom/{modelId}</c> endpoint.
    /// </summary>
    /// <param name="apiKey">The scenario.com API key to authenticate with.</param>
    /// <param name="apiSecret">The scenario.com API secret to authenticate with.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching models, ordered by display name.</returns>
    Task<IReadOnlyList<ScenarioModel>> GetTextToImageModelsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IScenarioModelCatalogService" />
/// <remarks>
/// Calls scenario.com's <c>models</c> endpoint. See https://docs.scenario.com for API details.
/// </remarks>
internal sealed class ScenarioModelCatalogService(HttpClient httpClient) : IScenarioModelCatalogService
{
    private const string BaseUrl = "https://api.cloud.scenario.com/v1";
    private const int PageSize = 500;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScenarioModel>> GetTextToImageModelsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        AuthenticationHeaderValue authorization = new(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));

        List<ScenarioModel> models = [];
        string? paginationToken = null;
        do
        {
            string url = $"{BaseUrl}/models?privacy=public&pageSize={PageSize}"
                + (paginationToken is null ? "" : $"&paginationToken={Uri.EscapeDataString(paginationToken)}");
            using HttpRequestMessage request = new(HttpMethod.Get, url) { Headers = { Authorization = authorization } };
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            ModelListResponse page = await response.Content.ReadFromJsonAsync<ModelListResponse>(AppJson.Options, cancellationToken)
                ?? throw new InvalidOperationException("Scenario.com returned an empty response when listing models.");

            models.AddRange(
                (page.Models ?? [])
                    .Where(model =>
                        model.Status == "trained"
                        && model.Type != "custom"
                        && (model.Capabilities?.Contains("txt2img") ?? false))
                    .Select(model => new ScenarioModel { Id = model.Id ?? "", Name = model.Name ?? model.Id ?? "" }));

            paginationToken = page.NextPaginationToken;
        }
        while (!string.IsNullOrEmpty(paginationToken));

        return models.OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private sealed record ModelListResponse
    {
        public List<ModelEntry>? Models { get; init; }

        public string? NextPaginationToken { get; init; }
    }

    private sealed record ModelEntry
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public string? Type { get; init; }

        public string? Status { get; init; }

        public List<string>? Capabilities { get; init; }
    }
}
