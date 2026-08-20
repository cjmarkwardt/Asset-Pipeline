namespace Markwardt.AssetPipeline.Client.ViewModels.Infrastructure;

/// <summary>
/// Resolves an <see cref="AssetNodeViewModel.OutputPath"/> (project-relative, possibly blank) into the full
/// folder path generated variants are written into.
/// </summary>
internal static class AssetOutputPaths
{
    private const string DefaultOutputPath = "Generated";

    /// <summary>
    /// Resolves <paramref name="outputPath"/> against <paramref name="projectPath"/>, falling back to a
    /// default "Generated" subfolder when it is blank.
    /// </summary>
    public static string Resolve(string projectPath, string outputPath) =>
        Path.GetFullPath(Path.Combine(projectPath, string.IsNullOrWhiteSpace(outputPath) ? DefaultOutputPath : outputPath));
}
