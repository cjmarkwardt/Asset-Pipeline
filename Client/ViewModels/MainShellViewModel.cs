namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Owns the set of currently open project tabs and which one is selected. Each tab's own state is fully
/// independent (see <see cref="IProjectTabFactory"/>); this type only ever coordinates the shared tab strip
/// around them (adding, selecting, reordering, closing).
/// </summary>
internal sealed partial class MainShellViewModel : ViewModelBase
{
    private readonly IProjectTabFactory tabFactory;
    private readonly IProjectService projectService;
    private readonly IDialogService dialogService;
    private readonly ILogger<MainShellViewModel> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainShellViewModel"/> class.
    /// </summary>
    public MainShellViewModel(HeaderViewModel header, IProjectTabFactory tabFactory, IProjectService projectService, IDialogService dialogService, ILogger<MainShellViewModel> logger)
    {
        Header = header;
        this.tabFactory = tabFactory;
        this.projectService = projectService;
        this.dialogService = dialogService;
        this.logger = logger;
        Header.ProjectOpened += OnProjectOpened;
    }

    [ObservableProperty]
    private ProjectTabViewModel? selectedTab;

    /// <summary>
    /// Gets the title bar's project launcher.
    /// </summary>
    public HeaderViewModel Header { get; }

    /// <summary>
    /// Gets the tab strip's backing collection - one entry per open project.
    /// </summary>
    public ObservableCollection<ProjectTabViewModel> Tabs { get; } = [];

    /// <summary>
    /// Restores the recently opened list and every project tab that was open when the app last closed.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Header.RefreshRecentProjectsAsync();

        // Sequential, not Task.WhenAll: SettingsService does an unlocked read-modify-write over one file
        // per call, and OpenAsync itself mutates the recents list as a side effect of opening - concurrent
        // opens here would race and silently drop entries from that list.
        foreach (ProjectInfo project in await projectService.GetOpenProjectsAsync())
        {
            await Header.OpenPathAsync(project.FullPath);
        }
    }

    /// <summary>
    /// Persists the exact set of open tabs and flushes every tab's pending save before the app exits.
    /// </summary>
    public async Task ShutdownAsync()
    {
        await projectService.SaveOpenProjectsAsync([.. Tabs.Select(tab => tab.Project.FullPath)]);

        foreach (ProjectTabViewModel tab in Tabs.ToList())
        {
            await DisposeTabAsync(tab, "during shutdown");
        }
    }

    [RelayCommand]
    private Task OpenOptionsAsync() => dialogService.ShowOptionsAsync();

    private async void OnProjectOpened(ProjectInfo project)
    {
        ProjectTabViewModel? existing = Tabs.FirstOrDefault(tab => tab.Project.FullPath == project.FullPath);
        if (existing is not null)
        {
            SelectedTab = existing;
            return;
        }

        ProjectTabViewModel tab = tabFactory.Create(project);
        tab.CloseRequested += OnTabCloseRequested;
        tab.MoveRequested += OnTabMoveRequested;
        Tabs.Add(tab);
        SelectedTab = tab;
        await tab.InitializeAsync();
    }

    /// <summary>
    /// Reorders a tab within the strip - a safe no-op if it's already at that end (offset would move it out
    /// of bounds). The tab strip's SelectedItem is two-way bound to SelectedTab, and re-sorting the bound
    /// collection out from under it - even via a single Move notification - still momentarily desyncs
    /// Avalonia's own selected-index tracking and nulls SelectedItem, which then writes back through the
    /// binding and nulls SelectedTab too. Restoring SelectedTab right after Move is the fix.
    /// </summary>
    private void OnTabMoveRequested(ProjectTabViewModel tab, int offset)
    {
        int index = Tabs.IndexOf(tab);
        int newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= Tabs.Count)
        {
            return;
        }

        ProjectTabViewModel? previousSelection = SelectedTab;
        Tabs.Move(index, newIndex);
        SelectedTab = previousSelection;
    }

    private async void OnTabCloseRequested(ProjectTabViewModel tab)
    {
        tab.CloseRequested -= OnTabCloseRequested;
        tab.MoveRequested -= OnTabMoveRequested;

        // Capture this before Remove, not after: the tab strip is two-way bound to SelectedTab, and
        // removing the currently-selected item from Tabs synchronously nulls SelectedTab as a side effect
        // of that binding.
        bool wasSelected = ReferenceEquals(SelectedTab, tab);
        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (wasSelected)
        {
            SelectedTab = index > 0 && index - 1 < Tabs.Count ? Tabs[index - 1] : Tabs.Count > 0 ? Tabs[0] : null;
        }

        await DisposeTabAsync(tab, "while closing it");
    }

    /// <summary>
    /// A disposal failure (e.g. a permissions error writing this project's data folder) must not become an
    /// unhandled exception on the caller's async void event handler, which would otherwise crash the whole
    /// app and take every other open project down with it.
    /// </summary>
    private async Task DisposeTabAsync(ProjectTabViewModel tab, string when)
    {
        try
        {
            await tab.DisposeAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to save project {ProjectPath} {When}", tab.Project.FullPath, when);
        }
    }
}
