namespace Markwardt.AssetPipeline.Tests.Core.Services;

/// <summary>
/// Tests for <see cref="Client.Core.Services.ProjectService"/>.
/// </summary>
public sealed class ProjectServiceTests : IDisposable
{
    private readonly string projectFolder;
    private readonly Mock<Client.Core.Services.ISettingsService> settingsService = new();
    private Client.Core.Models.AppSettings state = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectServiceTests"/> class, creating a real temporary
    /// folder with an <c>.astproj</c> subfolder to stand in for a valid project.
    /// </summary>
    public ProjectServiceTests()
    {
        projectFolder = Path.Combine(Path.GetTempPath(), $"astproj-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(projectFolder, Client.Core.Models.ProjectInfo.DataFolderName));

        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => state);
        settingsService
            .Setup(s => s.SaveAsync(It.IsAny<Client.Core.Models.AppSettings>(), It.IsAny<CancellationToken>()))
            .Callback<Client.Core.Models.AppSettings, CancellationToken>((settings, _) => state = settings)
            .Returns(Task.CompletedTask);
    }

    /// <inheritdoc />
    public void Dispose() => Directory.Delete(projectFolder, recursive: true);

    private Client.Core.Services.ProjectService CreateService() => new(settingsService.Object);

    [Fact]
    public async Task OpenAsyncReturnsNullForFolderWithoutDataFolder()
    {
        string invalidFolder = Path.Combine(Path.GetTempPath(), $"astproj-tests-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(invalidFolder);
        try
        {
            Client.Core.Services.ProjectService service = CreateService();

            Client.Core.Models.ProjectInfo? project = await service.OpenAsync(invalidFolder);

            Assert.Null(project);
        }
        finally
        {
            Directory.Delete(invalidFolder, recursive: true);
        }
    }

    [Fact]
    public async Task OpenAsyncReturnsProjectAndRecordsItAsMostRecentForValidFolder()
    {
        Client.Core.Services.ProjectService service = CreateService();

        Client.Core.Models.ProjectInfo? project = await service.OpenAsync(projectFolder);

        Assert.NotNull(project);
        Assert.Equal(Path.GetFullPath(projectFolder), project!.FullPath);
        Assert.Equal(Path.GetFullPath(projectFolder), Assert.Single(state.RecentProjectPaths));
    }

    [Fact]
    public async Task OpenAsyncMovesAlreadyRecentProjectToFront()
    {
        string otherFolder = Path.Combine(Path.GetTempPath(), $"astproj-tests-other-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(otherFolder, Client.Core.Models.ProjectInfo.DataFolderName));
        try
        {
            Client.Core.Services.ProjectService service = CreateService();
            await service.OpenAsync(projectFolder);
            await service.OpenAsync(otherFolder);

            await service.OpenAsync(projectFolder);

            Assert.Equal(Path.GetFullPath(projectFolder), state.RecentProjectPaths[0]);
            Assert.Equal(2, state.RecentProjectPaths.Count);
        }
        finally
        {
            Directory.Delete(otherFolder, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsyncAddsTheDataFolderAndRecordsItAsMostRecent()
    {
        string newFolder = Path.Combine(Path.GetTempPath(), $"astproj-tests-new-{Guid.NewGuid():N}");
        Directory.CreateDirectory(newFolder);
        try
        {
            Client.Core.Services.ProjectService service = CreateService();

            Client.Core.Models.ProjectInfo project = await service.CreateAsync(newFolder);

            Assert.Equal(Path.GetFullPath(newFolder), project.FullPath);
            Assert.True(Directory.Exists(Path.Combine(newFolder, Client.Core.Models.ProjectInfo.DataFolderName)));
            Assert.Equal(Path.GetFullPath(newFolder), Assert.Single(state.RecentProjectPaths));
        }
        finally
        {
            Directory.Delete(newFolder, recursive: true);
        }
    }

    [Fact]
    public async Task ForgetRecentAsyncRemovesProjectFromRecents()
    {
        Client.Core.Services.ProjectService service = CreateService();
        await service.OpenAsync(projectFolder);

        await service.ForgetRecentAsync(projectFolder);

        Assert.Empty(state.RecentProjectPaths);
    }

    [Fact]
    public async Task GetRecentProjectsAsyncFiltersOutFoldersThatNoLongerExist()
    {
        state = new Client.Core.Models.AppSettings
        {
            RecentProjectPaths = [Path.Combine(Path.GetTempPath(), $"astproj-tests-missing-{Guid.NewGuid():N}")],
        };
        Client.Core.Services.ProjectService service = CreateService();

        IReadOnlyList<Client.Core.Models.ProjectInfo> recents = await service.GetRecentProjectsAsync();

        Assert.Empty(recents);
    }

    [Fact]
    public async Task SaveOpenProjectsAsyncThenGetOpenProjectsAsyncRoundTrips()
    {
        Client.Core.Services.ProjectService service = CreateService();

        await service.SaveOpenProjectsAsync([projectFolder]);
        IReadOnlyList<Client.Core.Models.ProjectInfo> open = await service.GetOpenProjectsAsync();

        Assert.Equal(Path.GetFullPath(projectFolder), Assert.Single(open).FullPath);
    }

    [Fact]
    public async Task GetLastParentFolderAsyncReturnsNullWhenNeverSet()
    {
        Client.Core.Services.ProjectService service = CreateService();

        string? parent = await service.GetLastParentFolderAsync();

        Assert.Null(parent);
    }

    [Fact]
    public async Task SaveLastParentFolderAsyncThenGetLastParentFolderAsyncRoundTrips()
    {
        Client.Core.Services.ProjectService service = CreateService();

        await service.SaveLastParentFolderAsync(projectFolder);
        string? parent = await service.GetLastParentFolderAsync();

        Assert.Equal(Path.GetFullPath(projectFolder), parent);
    }
}
