namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="Client.ViewModels.MainShellViewModel"/>.
/// </summary>
public sealed class MainShellViewModelTests
{
    private readonly Mock<Client.Core.Services.IProjectService> projectService = new();
    private readonly Mock<Client.ViewModels.Infrastructure.IDialogService> dialogService = new();
    private readonly Mock<Client.Core.Services.IProjectDataStore> dataStore = new();
    private readonly Mock<Client.ViewModels.Infrastructure.IAssetVariantGenerator> variantGenerator = new();
    private readonly Mock<Client.Core.Services.ISettingsService> settingsService = new();
    private readonly Mock<Client.Core.Services.IScenarioModelCatalogService> modelCatalogService = new();
    private readonly Mock<Client.Core.Services.IScenarioModelPickerService> modelPickerService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainShellViewModelTests"/> class.
    /// </summary>
    public MainShellViewModelTests()
    {
        projectService.Setup(s => s.GetRecentProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Client.Core.Models.ProjectInfo>)[]);
        projectService.Setup(s => s.OpenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, CancellationToken _) => new Client.Core.Models.ProjectInfo { FullPath = path });
        dataStore.Setup(s => s.SaveTreeAsync(It.IsAny<string>(), It.IsAny<Client.Core.Models.ProjectNode>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings());
    }

    private Client.ViewModels.MainShellViewModel CreateShell()
    {
        Client.ViewModels.HeaderViewModel header = new(projectService.Object, dialogService.Object);
        Mock<Client.ViewModels.IProjectTabFactory> tabFactory = new();
        tabFactory.Setup(f => f.Create(It.IsAny<Client.Core.Models.ProjectInfo>()))
            .Returns((Client.Core.Models.ProjectInfo p) => new Client.ViewModels.ProjectTabViewModel(
                p, dataStore.Object, dialogService.Object, variantGenerator.Object, settingsService.Object, modelCatalogService.Object, modelPickerService.Object));

        return new Client.ViewModels.MainShellViewModel(
            header,
            tabFactory.Object,
            projectService.Object,
            dialogService.Object,
            new Mock<ILogger<Client.ViewModels.MainShellViewModel>>().Object);
    }

    [Fact]
    public async Task OpeningAProjectAddsAndSelectsANewTab()
    {
        Client.ViewModels.MainShellViewModel shell = CreateShell();

        await shell.Header.OpenPathAsync("/tmp/project-a");

        Client.ViewModels.ProjectTabViewModel tab = Assert.Single(shell.Tabs);
        Assert.Same(tab, shell.SelectedTab);
        Assert.Equal("/tmp/project-a", tab.Project.FullPath);
    }

    [Fact]
    public async Task OpeningTheSameProjectTwiceSelectsTheExistingTabInsteadOfDuplicating()
    {
        Client.ViewModels.MainShellViewModel shell = CreateShell();
        await shell.Header.OpenPathAsync("/tmp/project-a");
        Client.ViewModels.ProjectTabViewModel firstTab = shell.Tabs[0];
        await shell.Header.OpenPathAsync("/tmp/project-b");

        await shell.Header.OpenPathAsync("/tmp/project-a");

        Assert.Equal(2, shell.Tabs.Count);
        Assert.Same(firstTab, shell.SelectedTab);
    }

    [Fact]
    public async Task ClosingTheSelectedTabSelectsItsLeftNeighbor()
    {
        Client.ViewModels.MainShellViewModel shell = CreateShell();
        await shell.Header.OpenPathAsync("/tmp/project-a");
        await shell.Header.OpenPathAsync("/tmp/project-b");
        Client.ViewModels.ProjectTabViewModel firstTab = shell.Tabs[0];
        Client.ViewModels.ProjectTabViewModel secondTab = shell.Tabs[1];
        Assert.Same(secondTab, shell.SelectedTab);

        secondTab.CloseCommand.Execute(null);
        await Task.Delay(50);

        Assert.Single(shell.Tabs);
        Assert.Same(firstTab, shell.SelectedTab);
    }

    [Fact]
    public async Task MovingATabRightReordersTheTabStripAndKeepsItsSelection()
    {
        Client.ViewModels.MainShellViewModel shell = CreateShell();
        await shell.Header.OpenPathAsync("/tmp/project-a");
        await shell.Header.OpenPathAsync("/tmp/project-b");
        Client.ViewModels.ProjectTabViewModel firstTab = shell.Tabs[0];
        Client.ViewModels.ProjectTabViewModel secondTab = shell.Tabs[1];

        firstTab.MoveRightCommand.Execute(null);

        Assert.Same(firstTab, shell.Tabs[1]);
        Assert.Same(secondTab, shell.Tabs[0]);
        Assert.Same(secondTab, shell.SelectedTab);
    }
}
