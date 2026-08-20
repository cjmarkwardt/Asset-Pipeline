namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Generates a single 3D model from a concept image via the meshy.ai API.
/// </summary>
internal interface IModelGenerationService
{
    /// <summary>
    /// Generates one 3D model from <paramref name="conceptImage"/>.
    /// </summary>
    /// <param name="conceptImage">The concept image's raw bytes.</param>
    /// <param name="conceptImageMediaType">The concept image's MIME type (e.g. <c>image/png</c>).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The generated model's raw bytes, in glTF binary (<c>.glb</c>) format.</returns>
    Task<byte[]> GenerateAsync(byte[] conceptImage, string conceptImageMediaType, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IModelGenerationService" />
/// <remarks>
/// Calls meshy.ai's <c>image-to-3d</c> generation endpoint, polls the resulting task until it completes, and
/// downloads the resulting glTF binary model. See https://docs.meshy.ai for API details.
/// </remarks>
internal sealed class MeshyModelGenerationService(HttpClient httpClient, ISettingsService settingsService) : IModelGenerationService
{
    private const string BaseUrl = "https://api.meshy.ai/openapi/v1/image-to-3d";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<byte[]> GenerateAsync(byte[] conceptImage, string conceptImageMediaType, CancellationToken cancellationToken = default)
    {
        ApiSettings api = (await settingsService.LoadAsync(cancellationToken)).Api;
        if (string.IsNullOrWhiteSpace(api.MeshyApiKey))
        {
            throw new InvalidOperationException("Meshy.ai API credentials are not configured. Set them from the Options dialog.");
        }

        AuthenticationHeaderValue authorization = new("Bearer", api.MeshyApiKey);
        string imageDataUri = $"data:{conceptImageMediaType};base64,{Convert.ToBase64String(conceptImage)}";

        using HttpRequestMessage submitRequest = new(HttpMethod.Post, BaseUrl)
        {
            // Always smart-topology (clean, natively separated topology), never the standard/high-detail
            // model type - see https://docs.meshy.ai/en/api/image-to-3d.
            Content = JsonContent.Create(new { image_url = imageDataUri, model_type = "smart-topology", ai_model = "meshy-t2" }),
        };
        submitRequest.Headers.Authorization = authorization;
        using HttpResponseMessage submitResponse = await httpClient.SendAsync(submitRequest, cancellationToken);
        submitResponse.EnsureSuccessStatusCode();
        TaskCreatedResponse created = await submitResponse.Content.ReadFromJsonAsync<TaskCreatedResponse>(AppJson.Options, cancellationToken)
            ?? throw new InvalidOperationException("Meshy.ai returned an empty response when submitting the model generation task.");
        string taskId = created.Result
            ?? throw new InvalidOperationException("Meshy.ai did not return a task id for the model generation request.");

        string modelUrl = await PollForModelUrlAsync(taskId, authorization, cancellationToken);
        return await httpClient.GetByteArrayAsync(modelUrl, cancellationToken);
    }

    private async Task<string> PollForModelUrlAsync(string taskId, AuthenticationHeaderValue authorization, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + PollTimeout;
        while (true)
        {
            using HttpRequestMessage statusRequest = new(HttpMethod.Get, $"{BaseUrl}/{taskId}") { Headers = { Authorization = authorization } };
            using HttpResponseMessage statusResponse = await httpClient.SendAsync(statusRequest, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            TaskStatusResponse status = await statusResponse.Content.ReadFromJsonAsync<TaskStatusResponse>(AppJson.Options, cancellationToken)
                ?? throw new InvalidOperationException("Meshy.ai returned an empty response while polling the model generation task.");

            switch (status.Status)
            {
                case "SUCCEEDED":
                    return status.ModelUrls?.Glb
                        ?? throw new InvalidOperationException("Meshy.ai reported task success but returned no glTF model URL.");
                case "FAILED":
                case "CANCELED":
                    throw new InvalidOperationException($"Meshy.ai reported that the model generation task {status.Status.ToLowerInvariant()}.");
                default:
                    if (DateTimeOffset.UtcNow > deadline)
                    {
                        throw new TimeoutException("Timed out waiting for meshy.ai to finish generating the model.");
                    }

                    await Task.Delay(PollInterval, cancellationToken);
                    break;
            }
        }
    }

    private sealed record TaskCreatedResponse
    {
        public string? Result { get; init; }
    }

    private sealed record TaskStatusResponse
    {
        public required string Status { get; init; }

        [JsonPropertyName("model_urls")]
        public ModelUrls? ModelUrls { get; init; }
    }

    private sealed record ModelUrls
    {
        public string? Glb { get; init; }
    }
}
