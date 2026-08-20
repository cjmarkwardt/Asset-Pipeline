namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Generates a single image from a text prompt via the scenario.com API.
/// </summary>
internal interface IImageGenerationService
{
    /// <summary>
    /// Generates one image satisfying <paramref name="prompt"/>.
    /// </summary>
    /// <param name="prompt">The prompt describing the image to generate.</param>
    /// <param name="modelId">
    /// The scenario.com model to generate with, resolved from the requesting node's own generation model
    /// setting or, if unset, the nearest ancestor's. <see langword="null"/> or blank if none of them has one
    /// set.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The generated image's raw bytes.</returns>
    Task<byte[]> GenerateAsync(string prompt, string? modelId, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IImageGenerationService" />
/// <remarks>
/// Calls scenario.com's <c>txt2img</c> generation endpoint, polls the resulting job until it completes, and
/// downloads the resulting asset. See https://docs.scenario.com for API details.
/// </remarks>
internal sealed class ScenarioImageGenerationService(HttpClient httpClient, ISettingsService settingsService) : IImageGenerationService
{
    private const string BaseUrl = "https://api.cloud.scenario.com/v1";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<byte[]> GenerateAsync(string prompt, string? modelId, CancellationToken cancellationToken = default)
    {
        ApiSettings api = (await settingsService.LoadAsync(cancellationToken)).Api;
        if (string.IsNullOrWhiteSpace(api.ScenarioApiKey) || string.IsNullOrWhiteSpace(api.ScenarioApiSecret))
        {
            throw new InvalidOperationException("Scenario.com API credentials are not configured. Set them from the Options dialog.");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException(
                "A scenario.com generation model is not configured for this node or any of its ancestors. Set one on the root node or an ancestor group/node.");
        }

        AuthenticationHeaderValue authorization = new(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.ScenarioApiKey}:{api.ScenarioApiSecret}")));

        using HttpRequestMessage submitRequest = new(HttpMethod.Post, $"{BaseUrl}/generate/txt2img")
        {
            Content = JsonContent.Create(new
            {
                prompt,
                modelId,
                numSamples = 1,
                numInferenceSteps = 30,
                guidance = 7,
                width = 1024,
                height = 1024,
            }),
        };
        submitRequest.Headers.Authorization = authorization;
        using HttpResponseMessage submitResponse = await httpClient.SendAsync(submitRequest, cancellationToken);
        submitResponse.EnsureSuccessStatusCode();
        JobEnvelope submitted = await submitResponse.Content.ReadFromJsonAsync<JobEnvelope>(AppJson.Options, cancellationToken)
            ?? throw new InvalidOperationException("Scenario.com returned an empty response when submitting the generation job.");
        string jobId = submitted.Job?.JobId
            ?? throw new InvalidOperationException("Scenario.com did not return a job id for the generation request.");

        string assetId = await PollForAssetIdAsync(jobId, authorization, cancellationToken);

        using HttpRequestMessage assetRequest = new(HttpMethod.Get, $"{BaseUrl}/assets/{assetId}") { Headers = { Authorization = authorization } };
        using HttpResponseMessage assetResponse = await httpClient.SendAsync(assetRequest, cancellationToken);
        assetResponse.EnsureSuccessStatusCode();
        AssetEnvelope asset = await assetResponse.Content.ReadFromJsonAsync<AssetEnvelope>(AppJson.Options, cancellationToken)
            ?? throw new InvalidOperationException("Scenario.com returned an empty response when fetching the generated asset.");
        string assetUrl = asset.Asset?.Url
            ?? throw new InvalidOperationException("Scenario.com did not return a download URL for the generated asset.");

        return await httpClient.GetByteArrayAsync(assetUrl, cancellationToken);
    }

    private async Task<string> PollForAssetIdAsync(string jobId, AuthenticationHeaderValue authorization, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + PollTimeout;
        while (true)
        {
            using HttpRequestMessage statusRequest = new(HttpMethod.Get, $"{BaseUrl}/jobs/{jobId}") { Headers = { Authorization = authorization } };
            using HttpResponseMessage statusResponse = await httpClient.SendAsync(statusRequest, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            JobEnvelope envelope = await statusResponse.Content.ReadFromJsonAsync<JobEnvelope>(AppJson.Options, cancellationToken)
                ?? throw new InvalidOperationException("Scenario.com returned an empty response while polling the generation job.");

            switch (envelope.Job?.Status)
            {
                case "success":
                    string? assetId = envelope.Job.Metadata?.AssetIds?.FirstOrDefault();
                    return assetId ?? throw new InvalidOperationException("Scenario.com reported job success but returned no generated asset.");
                case "failure":
                    throw new InvalidOperationException("Scenario.com reported that the image generation job failed.");
                default:
                    if (DateTimeOffset.UtcNow > deadline)
                    {
                        throw new TimeoutException("Timed out waiting for scenario.com to finish generating the image.");
                    }

                    await Task.Delay(PollInterval, cancellationToken);
                    break;
            }
        }
    }

    private sealed record JobEnvelope
    {
        public JobStatus? Job { get; init; }
    }

    private sealed record JobStatus
    {
        public string? JobId { get; init; }

        public string? Status { get; init; }

        public JobMetadata? Metadata { get; init; }
    }

    private sealed record JobMetadata
    {
        public List<string>? AssetIds { get; init; }
    }

    private sealed record AssetEnvelope
    {
        public AssetDetails? Asset { get; init; }
    }

    private sealed record AssetDetails
    {
        public string? Url { get; init; }
    }
}
