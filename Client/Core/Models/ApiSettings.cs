namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// Persisted credentials for the third-party generation APIs the client calls out to.
/// </summary>
internal sealed record ApiSettings
{
    /// <summary>
    /// Gets the API key issued by scenario.com, used together with <see cref="ScenarioApiSecret"/> to
    /// authenticate image generation requests.
    /// </summary>
    public string ScenarioApiKey { get; init; } = "";

    /// <summary>
    /// Gets the API secret issued by scenario.com.
    /// </summary>
    public string ScenarioApiSecret { get; init; } = "";

    /// <summary>
    /// Gets the API key issued by meshy.ai, used to authenticate 3D model generation requests.
    /// </summary>
    public string MeshyApiKey { get; init; } = "";
}
