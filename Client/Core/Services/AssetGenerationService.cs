namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Generates a single asset variant of a given kind, writing its output file(s) under a project-relative
/// output path. Node-agnostic: callers resolve which combination of context/settings/output location a
/// specific node wants generated and hand this service only the plain values it needs.
/// </summary>
internal interface IAssetGenerationService
{
    /// <summary>
    /// Generates a text variant and writes it to a new file under <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="outputDirectory">The full path of the folder to write the generated file into.</param>
    /// <param name="fileBaseName">The file name (without extension) to write the generated file as.</param>
    /// <param name="request">The context and constraints the generated text must satisfy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The generated file's name (not full path).</returns>
    Task<string> GenerateTextAsync(string outputDirectory, string fileBaseName, TextGenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an image variant and writes it to a new file under <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="outputDirectory">The full path of the folder to write the generated file into.</param>
    /// <param name="fileBaseName">The file name (without extension) to write the generated file as.</param>
    /// <param name="prompt">The prompt describing the image to generate.</param>
    /// <param name="modelId">
    /// The scenario.com model to generate with, resolved from the requesting node's own generation model
    /// setting or, if unset, the nearest ancestor's. <see langword="null"/> or blank if none of them has one
    /// set.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The generated file's name (not full path).</returns>
    Task<string> GenerateImageAsync(string outputDirectory, string fileBaseName, string prompt, string? modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a 3D model variant from a concept image already written on disk, and writes the model to a
    /// new file under <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="outputDirectory">The full path of the folder to write the generated file into.</param>
    /// <param name="fileBaseName">The file name (without extension) to write the generated file as.</param>
    /// <param name="conceptImagePath">The full path of the concept image to generate the model from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The generated file's name (not full path).</returns>
    Task<string> GenerateModelAsync(string outputDirectory, string fileBaseName, string conceptImagePath, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IAssetGenerationService" />
internal sealed class AssetGenerationService(
    ITextGenerationService textGenerationService,
    IImageGenerationService imageGenerationService,
    IModelGenerationService modelGenerationService) : IAssetGenerationService
{
    private readonly Dictionary<string, string> imageMediaTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
    };

    /// <inheritdoc />
    public async Task<string> GenerateTextAsync(string outputDirectory, string fileBaseName, TextGenerationRequest request, CancellationToken cancellationToken = default)
    {
        string text = await textGenerationService.GenerateAsync(request, cancellationToken);
        string fileName = $"{fileBaseName}{(request.IsJsonFormatted ? ".json" : ".txt")}";
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, fileName), text, cancellationToken);
        return fileName;
    }

    /// <inheritdoc />
    public async Task<string> GenerateImageAsync(string outputDirectory, string fileBaseName, string prompt, string? modelId, CancellationToken cancellationToken = default)
    {
        byte[] image = await imageGenerationService.GenerateAsync(prompt, modelId, cancellationToken);
        string fileName = $"{fileBaseName}.png";
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, fileName), image, cancellationToken);
        return fileName;
    }

    /// <inheritdoc />
    public async Task<string> GenerateModelAsync(string outputDirectory, string fileBaseName, string conceptImagePath, CancellationToken cancellationToken = default)
    {
        byte[] conceptImage = await File.ReadAllBytesAsync(conceptImagePath, cancellationToken);
        string mediaType = imageMediaTypesByExtension.GetValueOrDefault(Path.GetExtension(conceptImagePath), "image/png");
        byte[] model = await modelGenerationService.GenerateAsync(conceptImage, mediaType, cancellationToken);
        string fileName = $"{fileBaseName}.glb";
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, fileName), model, cancellationToken);
        return fileName;
    }
}
