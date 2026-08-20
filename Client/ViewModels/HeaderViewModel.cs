namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Drives the title bar's project launcher: opening a folder directly, or picking one from the recently
/// opened list.
/// </summary>
internal sealed partial class HeaderViewModel(IProjectService projectService, IDialogService dialogService) : ViewModelBase
{
    [ObservableProperty]
    private bool isRecentMenuOpen;

    /// <summary>
    /// Raised whenever a project folder has been successfully opened.
    /// </summary>
    public event Action<ProjectInfo>? ProjectOpened;

    /// <summary>
    /// Gets the recently opened projects, backing the title bar's "Open Recent" dropdown.
    /// </summary>
    public ObservableCollection<ProjectInfo> RecentProjects { get; } = [];

    /// <summary>
    /// Opens the project at <paramref name="path"/> with no picker UI involved. Shared by
    /// <see cref="BrowseForFolderAsync"/>/<see cref="OpenRecentAsync"/>, and by
    /// <see cref="MainShellViewModel.InitializeAsync"/> for restoring previously open tabs on launch.
    /// </summary>
    public Task OpenPathAsync(string path) => OpenProjectAsync(path);

    /// <summary>
    /// Reloads <see cref="RecentProjects"/> from persisted settings.
    /// </summary>
    public async Task RefreshRecentProjectsAsync()
    {
        IReadOnlyList<ProjectInfo> recents = await projectService.GetRecentProjectsAsync();
        RecentProjects.Clear();
        foreach (ProjectInfo project in recents)
        {
            RecentProjects.Add(project);
        }
    }

    /// <summary>
    /// Closes the "Open Recent" dropdown without opening anything.
    /// </summary>
    public void CloseRecentMenu() => IsRecentMenuOpen = false;

    [RelayCommand]
    private async Task BrowseForFolderAsync()
    {
        string? path = await dialogService.PickFolderAsync(await projectService.GetLastParentFolderAsync());
        if (path is null)
        {
            return;
        }

        if (Path.GetDirectoryName(path) is { Length: > 0 } parentDirectory)
        {
            await projectService.SaveLastParentFolderAsync(parentDirectory);
        }

        await OpenProjectAsync(path);
    }

    [RelayCommand]
    private void ToggleRecentMenu() => IsRecentMenuOpen = !IsRecentMenuOpen;

    [RelayCommand]
    private async Task OpenRecentAsync(ProjectInfo project)
    {
        IsRecentMenuOpen = false;
        await OpenProjectAsync(project.FullPath);
    }

    [RelayCommand]
    private async Task RemoveRecentAsync(ProjectInfo project)
    {
        await projectService.ForgetRecentAsync(project.FullPath);
        await RefreshRecentProjectsAsync();
    }

    private async Task OpenProjectAsync(string path)
    {
        ProjectInfo? project = await projectService.OpenAsync(path);
        if (project is null)
        {
            bool shouldCreate = await dialogService.ShowConfirmAsync(
                "Project Not Found",
                $"'{path}' does not exist as an Asset Pipeline project. Would you like to create one there?",
                "Create Project");
            if (!shouldCreate)
            {
                return;
            }

            project = await projectService.CreateAsync(path);
        }

        ProjectOpened?.Invoke(project);
        await RefreshRecentProjectsAsync();
    }
}
