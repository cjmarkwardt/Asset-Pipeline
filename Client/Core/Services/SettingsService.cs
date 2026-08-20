namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Loads and persists the application's user-scoped <see cref="AppSettings"/>.
/// </summary>
internal interface ISettingsService
{
    /// <summary>
    /// Loads the current settings, or a fresh default instance if none have been saved yet.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <paramref name="settings"/>, overwriting whatever was previously saved.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ISettingsService" />
internal sealed class SettingsService : ISettingsService
{
    private readonly string settingsFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class, ensuring the application
    /// data directory it stores settings in exists.
    /// </summary>
    public SettingsService()
    {
        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            "AssetPipeline");
        Directory.CreateDirectory(appDataDirectory);
        settingsFilePath = Path.Combine(appDataDirectory, "settings.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            await using FileStream stream = File.OpenRead(settingsFilePath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, AppJson.Options, cancellationToken)
                   ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.Create(settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, AppJson.Options, cancellationToken);
    }
}
