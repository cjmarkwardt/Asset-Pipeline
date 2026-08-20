namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Owns one open project's own isolated state: its node tree, the sidebar/content selection within it, and
/// persisting edits back to that project's own data folder. Nothing here is shared with any other open
/// project tab.
/// </summary>
internal sealed partial class ProjectTabViewModel : ViewModelBase, IAsyncDisposable
{
    private const string RootNodeId = "root";
    private static readonly TimeSpan SaveDebounceDelay = TimeSpan.FromMilliseconds(400);

    private readonly IProjectDataStore dataStore;
    private readonly IDialogService dialogService;
    private readonly IAssetVariantGenerator variantGenerator;
    private readonly ISettingsService settingsService;
    private readonly IScenarioModelCatalogService modelCatalogService;
    private readonly IScenarioModelPickerService modelPickerService;
    private readonly ScenarioModel inheritScenarioModelOption = new() { Id = "", Name = "(Inherit From Parent)" };
    private CancellationTokenSource? saveDebounceCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectTabViewModel"/> class with a placeholder root
    /// node; call <see cref="InitializeAsync"/> to load the project's real, persisted tree.
    /// </summary>
    public ProjectTabViewModel(
        ProjectInfo project,
        IProjectDataStore dataStore,
        IDialogService dialogService,
        IAssetVariantGenerator variantGenerator,
        ISettingsService settingsService,
        IScenarioModelCatalogService modelCatalogService,
        IScenarioModelPickerService modelPickerService)
    {
        Project = project;
        this.dataStore = dataStore;
        this.dialogService = dialogService;
        this.variantGenerator = variantGenerator;
        this.settingsService = settingsService;
        this.modelCatalogService = modelCatalogService;
        this.modelPickerService = modelPickerService;
        RootNode = new RootNodeViewModel(RootNodeId, "Project");
        selectedNode = RootNode;
        RootNode.Changed += OnTreeChanged;
        AvailableScenarioModels = [inheritScenarioModelOption];
    }

    /// <summary>Sidebar column width, bound two-way from the GridSplitter in <see cref="Views.ProjectTabView"/> - kept in-memory only, per tab.</summary>
    [ObservableProperty]
    private GridLength sidebarWidth = new(320);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateVariantsCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLinkCommand))]
    private ProjectNodeViewModel selectedNode;

    [ObservableProperty]
    private bool isLoadingScenarioModels;

    [ObservableProperty]
    private string? scenarioModelLoadError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PickScenarioModelCommand))]
    private bool isPickingScenarioModel;

    /// <summary>
    /// Raised by <see cref="CloseCommand"/> - see <see cref="MainShellViewModel"/>, which removes this tab
    /// from its own tab strip in response.
    /// </summary>
    public event Action<ProjectTabViewModel>? CloseRequested;

    /// <summary>
    /// Raised by <see cref="MoveLeftCommand"/>/<see cref="MoveRightCommand"/> (offset -1/+1) - see
    /// <see cref="MainShellViewModel"/>, which reorders this tab within its own tab strip in response.
    /// </summary>
    public event Action<ProjectTabViewModel, int>? MoveRequested;

    /// <summary>
    /// Gets the project this tab represents.
    /// </summary>
    public ProjectInfo Project { get; }

    /// <summary>
    /// Gets this project's node tree root. Reassigned once <see cref="InitializeAsync"/> loads the
    /// project's real, persisted tree.
    /// </summary>
    public RootNodeViewModel RootNode { get; private set; }

    /// <summary>
    /// Gets the tab strip label for this project.
    /// </summary>
    public string Title => Project.Name;

    /// <summary>
    /// Gets the full path shown as this tab's tooltip.
    /// </summary>
    public string TooltipPath => Project.FullPath;

    /// <summary>
    /// Gets the sidebar tree's single top-level item collection, containing only <see cref="RootNode"/> -
    /// the whole project tree, root included, renders through one recursive TreeView.
    /// </summary>
    public IReadOnlyList<ProjectNodeViewModel> RootNodeItems => [RootNode];

    /// <summary>
    /// Gets the text-to-image scenario.com models available to pick from when overriding a non-root node's
    /// generation model, populated by <see cref="RefreshScenarioModelsCommand"/>. Always starts with a
    /// sentinel "inherit from parent" entry (an empty <see cref="ScenarioModel.Id"/>).
    /// </summary>
    public ObservableCollection<ScenarioModel> AvailableScenarioModels { get; }

    /// <summary>
    /// Gets the text-to-image scenario.com models available to pick from for <see cref="RootNode"/>'s own
    /// generation model, populated alongside <see cref="AvailableScenarioModels"/> by
    /// <see cref="RefreshScenarioModelsCommand"/> - unlike that collection, this one never includes the
    /// "inherit from parent" sentinel, since the root node has no parent to inherit from.
    /// </summary>
    public ObservableCollection<ScenarioModel> RootScenarioModelOptions { get; } = [];

    /// <summary>
    /// Loads this project's real, persisted node tree, replacing the placeholder one created in the
    /// constructor, then fetches the available scenario.com generation models.
    /// </summary>
    public async Task InitializeAsync()
    {
        ProjectNode model = await dataStore.LoadTreeAsync(Project.FullPath);
        if (model is not RootNode rootModel)
        {
            throw new InvalidOperationException($"A project's persisted tree must be rooted at a '{nameof(RootNode)}'.");
        }

        RootNode.Changed -= OnTreeChanged;
        RootNode = RootNodeViewModel.FromModel(rootModel, Project.FullPath);
        ProjectNodeViewModel.ResolveLinks(RootNode);
        RootNode.Changed += OnTreeChanged;
        OnPropertyChanged(nameof(RootNode));
        OnPropertyChanged(nameof(RootNodeItems));
        SelectedNode = RootNode;

        await RefreshScenarioModelsAsync();
    }

    /// <summary>
    /// Flushes any pending debounced save immediately - called once by <see cref="MainShellViewModel"/>
    /// when this tab closes or the app shuts down, so an edit made just before that moment is never lost.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        saveDebounceCts?.Cancel();
        await dataStore.SaveTreeAsync(Project.FullPath, RootNode.ToModel());
    }

    /// <summary>
    /// Renames <paramref name="node"/> to a name entered through a dialog - a no-op for the root node,
    /// which cannot be renamed.
    /// </summary>
    [RelayCommand]
    private async Task RenameNodeAsync(ProjectNodeViewModel node)
    {
        if (node.Parent is null)
        {
            return;
        }

        string? newName = await dialogService.ShowTextInputAsync("Rename Node", "Enter a new name for this node.", node.Name);
        if (!string.IsNullOrWhiteSpace(newName))
        {
            node.Name = newName;
        }
    }

    /// <summary>
    /// Deletes <paramref name="node"/> - a no-op for the root node, which cannot be deleted. Moves the
    /// current selection up to <paramref name="node"/>'s own parent first, if the selection is
    /// <paramref name="node"/> itself or one of its descendants.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteNode))]
    private void DeleteNode(ProjectNodeViewModel? node)
    {
        if (node?.Parent is not GroupNodeViewModel parent)
        {
            return;
        }

        if (ReferenceEquals(SelectedNode, node) || IsDescendantOf(SelectedNode, node))
        {
            SelectedNode = parent;
        }

        parent.RemoveChild(node);
    }

    private static bool CanDeleteNode(ProjectNodeViewModel? node) => node?.Parent is not null;

    private static bool IsDescendantOf(ProjectNodeViewModel node, ProjectNodeViewModel ancestor)
    {
        for (ProjectNodeViewModel? current = node.Parent; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private async Task UploadReferenceFileAsync()
    {
        string? sourcePath = await dialogService.PickFileAsync(Project.FullPath);
        if (sourcePath is null)
        {
            return;
        }

        string relativePath = await dataStore.StoreReferenceFileAsync(Project.FullPath, sourcePath);
        SelectedNode.AddReferenceFile(new ReferenceFileViewModel(
            Path.GetFileNameWithoutExtension(sourcePath),
            ReferenceFileSource.Stored,
            relativePath,
            Path.Combine(Project.FullPath, ProjectInfo.DataFolderName, relativePath)));
    }

    [RelayCommand]
    private async Task LinkReferenceFileAsync()
    {
        string? fullPath = await dialogService.PickFileAsync(Project.FullPath);
        if (fullPath is null)
        {
            return;
        }

        string relativePath = Path.GetRelativePath(Project.FullPath, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            await dialogService.ShowMessageAsync("Cannot Link File", "The selected file must be located inside the project folder.");
            return;
        }

        SelectedNode.AddReferenceFile(new ReferenceFileViewModel(
            Path.GetFileNameWithoutExtension(fullPath),
            ReferenceFileSource.ProjectPath,
            relativePath,
            fullPath));
    }

    [RelayCommand]
    private async Task RefreshScenarioModelsAsync()
    {
        ApiSettings api = (await settingsService.LoadAsync()).Api;
        if (string.IsNullOrWhiteSpace(api.ScenarioApiKey) || string.IsNullOrWhiteSpace(api.ScenarioApiSecret))
        {
            ScenarioModelLoadError = "Set a scenario.com API key and secret from the Options dialog to load available generation models.";
            return;
        }

        IsLoadingScenarioModels = true;
        ScenarioModelLoadError = null;
        try
        {
            IReadOnlyList<ScenarioModel> models = await modelCatalogService.GetTextToImageModelsAsync(api.ScenarioApiKey, api.ScenarioApiSecret);
            AvailableScenarioModels.Clear();
            AvailableScenarioModels.Add(inheritScenarioModelOption);
            RootScenarioModelOptions.Clear();
            foreach (ScenarioModel model in models)
            {
                AvailableScenarioModels.Add(model);
                RootScenarioModelOptions.Add(model);
            }

            if (string.IsNullOrWhiteSpace(RootNode.ScenarioModelId) && models.Count > 0)
            {
                RootNode.ScenarioModelId = models[0].Id;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            ScenarioModelLoadError = $"Could not load scenario.com models: {exception.Message}";
        }
        finally
        {
            IsLoadingScenarioModels = false;
        }
    }

    /// <summary>
    /// Backs the "Auto" button next to a node's generation model picker (root included): asks Claude to pick
    /// the best of the real (non-sentinel) entries in <see cref="AvailableScenarioModels"/> for
    /// <paramref name="node"/>'s own aggregated context, then sets its
    /// <see cref="ProjectNodeViewModel.ScenarioModelId"/> to the result.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPickScenarioModel))]
    private async Task PickScenarioModelAsync(ProjectNodeViewModel node)
    {
        List<ScenarioModel> realModels = [.. AvailableScenarioModels.Where(model => !string.IsNullOrEmpty(model.Id))];
        if (realModels.Count == 0)
        {
            await dialogService.ShowMessageAsync("No Models Available", "Refresh the available scenario.com models before using Auto.");
            return;
        }

        IsPickingScenarioModel = true;
        try
        {
            AggregatedContext context = NodeContextAggregator.Collect(node);
            node.ScenarioModelId = await modelPickerService.PickModelAsync(context.Text, realModels);
        }
        catch (InvalidOperationException exception)
        {
            await dialogService.ShowMessageAsync("Cannot Pick Model", exception.Message);
        }
        finally
        {
            IsPickingScenarioModel = false;
        }
    }

    private bool CanPickScenarioModel(ProjectNodeViewModel? node) => !IsPickingScenarioModel;

    [RelayCommand]
    private async Task PickOutputPathAsync()
    {
        if (SelectedNode is not AssetNodeViewModel asset)
        {
            return;
        }

        string? fullPath = await dialogService.PickFolderAsync(Project.FullPath);
        if (fullPath is null)
        {
            return;
        }

        string relativePath = Path.GetRelativePath(Project.FullPath, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            await dialogService.ShowMessageAsync("Cannot Set Output Path", "The selected folder must be located inside the project folder.");
            return;
        }

        asset.OutputPath = relativePath;
    }

    [RelayCommand(CanExecute = nameof(CanGenerateVariants))]
    private async Task GenerateVariantsAsync()
    {
        if (SelectedNode is not AssetNodeViewModel asset || asset.GenerateCount < 1)
        {
            return;
        }

        try
        {
            await variantGenerator.GenerateNewVariantsAsync(Project.FullPath, asset, asset.GenerateCount);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await dialogService.ShowMessageAsync("Cannot Generate Variants", ex.Message);
        }
    }

    private bool CanGenerateVariants() => SelectedNode is AssetNodeViewModel;

    [RelayCommand]
    private async Task RegenerateVariantAsync(AssetVariantViewModel variant)
    {
        if (SelectedNode is AssetNodeViewModel asset)
        {
            await variantGenerator.RegenerateAsync(Project.FullPath, asset, variant);
        }
    }

    [RelayCommand]
    private async Task RegenerateMeshOnlyAsync(MeshVariantViewModel variant)
    {
        if (SelectedNode is MeshNodeViewModel mesh)
        {
            await variantGenerator.RegenerateMeshOnlyAsync(Project.FullPath, mesh, variant);
        }
    }

    [RelayCommand]
    private async Task ApproveConceptAsync(MeshVariantViewModel variant)
    {
        if (SelectedNode is MeshNodeViewModel mesh)
        {
            await variantGenerator.ApproveConceptAsync(Project.FullPath, mesh, variant);
        }
    }

    [RelayCommand]
    private async Task RenameVariantAsync(AssetVariantViewModel variant)
    {
        string? newName = await dialogService.ShowTextInputAsync("Rename Variant", "Enter a new name for this variant.", variant.Name);
        if (!string.IsNullOrWhiteSpace(newName))
        {
            variant.Name = newName;
        }
    }

    [RelayCommand]
    private void DeleteVariant(AssetVariantViewModel variant)
    {
        if (SelectedNode is AssetNodeViewModel asset && asset.Variants.Remove(variant))
        {
            asset.NotifyVariantsChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddLink))]
    private async Task AddLinkAsync()
    {
        ProjectNodeViewModel current = SelectedNode;
        List<ProjectNodeViewModel> candidates = [.. EnumerateAllNodes(RootNode).Where(current.CanLinkTo)];
        if (candidates.Count == 0)
        {
            await dialogService.ShowMessageAsync("No Nodes To Link", "There are no other nodes this node can be linked to.");
            return;
        }

        ProjectNodeViewModel? target = await dialogService.PickNodeAsync("Link To Node", candidates);
        if (target is not null)
        {
            current.AddLink(target);
        }
    }

    private bool CanAddLink() => SelectedNode is not null;

    private static IEnumerable<ProjectNodeViewModel> EnumerateAllNodes(ProjectNodeViewModel node)
    {
        yield return node;
        foreach (ProjectNodeViewModel child in node.Children)
        {
            foreach (ProjectNodeViewModel descendant in EnumerateAllNodes(child))
            {
                yield return descendant;
            }
        }
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this);

    [RelayCommand]
    private void MoveLeft() => MoveRequested?.Invoke(this, -1);

    [RelayCommand]
    private void MoveRight() => MoveRequested?.Invoke(this, 1);

    /// <summary>
    /// The sidebar tree's <c>SelectedItem</c> is bound two-way to <see cref="SelectedNode"/>, and reports
    /// <see langword="null"/> whenever it has no selection of its own (e.g. the previously selected node was
    /// just removed) - falling back to <see cref="RootNode"/> here keeps <see cref="SelectedNode"/> itself
    /// always non-null, which the rest of this class (and the content area) depends on.
    /// </summary>
    partial void OnSelectedNodeChanged(ProjectNodeViewModel value)
    {
        if (value is null)
        {
            SelectedNode = RootNode;
        }
    }

    private void OnTreeChanged()
    {
        saveDebounceCts?.Cancel();
        CancellationTokenSource cts = new();
        saveDebounceCts = cts;
        _ = DebouncedSaveAsync(cts.Token);
    }

    private async Task DebouncedSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SaveDebounceDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await dataStore.SaveTreeAsync(Project.FullPath, RootNode.ToModel());
    }
}
