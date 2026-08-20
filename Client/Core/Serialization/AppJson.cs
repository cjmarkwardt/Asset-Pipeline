namespace Markwardt.AssetPipeline.Client.Core.Serialization;

/// <summary>
/// Shared JSON serialization settings used to persist application data.
/// </summary>
internal static class AppJson
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used whenever application data is serialized to or
    /// deserialized from JSON.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
