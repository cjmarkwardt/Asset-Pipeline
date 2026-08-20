namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Creates a project tab's view model. Each call produces a fully independent instance (its own node tree,
/// its own selection state, its own save pipeline), isolating one open project from every other open
/// project.
/// </summary>
internal interface IProjectTabFactory
{
    /// <summary>
    /// Creates a new, independent tab for <paramref name="project"/>.
    /// </summary>
    ProjectTabViewModel Create(ProjectInfo project);
}

/// <inheritdoc cref="IProjectTabFactory" />
internal sealed class ProjectTabFactory(
    IProjectDataStore dataStore,
    IDialogService dialogService,
    IAssetVariantGenerator variantGenerator,
    ISettingsService settingsService,
    IScenarioModelCatalogService modelCatalogService,
    IScenarioModelPickerService modelPickerService) : IProjectTabFactory
{
    /// <inheritdoc />
    public ProjectTabViewModel Create(ProjectInfo project) =>
        new(project, dataStore, dialogService, variantGenerator, settingsService, modelCatalogService, modelPickerService);
}
