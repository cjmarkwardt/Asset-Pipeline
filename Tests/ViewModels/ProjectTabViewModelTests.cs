namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="Client.ViewModels.ProjectTabViewModel"/>.
/// </summary>
public sealed class ProjectTabViewModelTests
{
    private readonly Mock<Client.Core.Services.IProjectDataStore> dataStore = new();
    private readonly Mock<Client.ViewModels.Infrastructure.IDialogService> dialogService = new();
    private readonly Mock<Client.ViewModels.Infrastructure.IAssetVariantGenerator> variantGenerator = new();
    private readonly Mock<Client.Core.Services.ISettingsService> settingsService = new();
    private readonly Mock<Client.Core.Services.IScenarioModelCatalogService> modelCatalogService = new();
    private readonly Mock<Client.Core.Services.IScenarioModelPickerService> modelPickerService = new();
    private readonly Client.Core.Models.ProjectInfo project = new() { FullPath = "/tmp/some-project" };

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectTabViewModelTests"/> class.
    /// </summary>
    public ProjectTabViewModelTests()
    {
        dataStore
            .Setup(s => s.SaveTreeAsync(It.IsAny<string>(), It.IsAny<Client.Core.Models.ProjectNode>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings());
    }

    private Client.ViewModels.ProjectTabViewModel CreateTab() =>
        new(project, dataStore.Object, dialogService.Object, variantGenerator.Object, settingsService.Object, modelCatalogService.Object, modelPickerService.Object);

    [Fact]
    public void RootNodeStartsSelectedAndUndeletable()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        Assert.Same(tab.RootNode, tab.SelectedNode);
        Assert.Same(tab.RootNode, Assert.Single(tab.RootNodeItems));
        Assert.False(tab.DeleteNodeCommand.CanExecute(tab.RootNode));
    }

    [Fact]
    public void DeleteNodeReparentsSelectionToItsFormerParent()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        tab.RootNode.AddChild(group);
        tab.SelectedNode = group;

        tab.DeleteNodeCommand.Execute(group);

        Assert.Same(tab.RootNode, tab.SelectedNode);
        Assert.Empty(tab.RootNode.Children);
        Assert.Null(group.Parent);
    }

    [Fact]
    public void DeleteNodeReparentsSelectionWhenDeletingAnAncestorOfTheSelection()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        Client.ViewModels.TextNodeViewModel child = new("t1", "Hero Bio");
        tab.RootNode.AddChild(group);
        group.AddChild(child);
        tab.SelectedNode = child;

        tab.DeleteNodeCommand.Execute(group);

        Assert.Same(tab.RootNode, tab.SelectedNode);
        Assert.Empty(tab.RootNode.Children);
    }

    [Fact]
    public async Task DisposeAsyncFlushesThePendingTreeToTheDataStore()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        tab.RootNode.AddChild(new Client.ViewModels.GroupNodeViewModel("g1", "Characters"));

        await tab.DisposeAsync();

        dataStore.Verify(
            s => s.SaveTreeAsync(project.FullPath, It.Is<Client.Core.Models.ProjectNode>(root => ((Client.Core.Models.RootNode)root).Children.Count == 1), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsyncReplacesThePlaceholderRootWithThePersistedTree()
    {
        Client.Core.Models.ProjectNode persisted = new Client.Core.Models.RootNode
        {
            Id = "root",
            Name = "Project",
            Children = [new Client.Core.Models.GroupNode { Id = "g1", Name = "Characters" }],
        };
        dataStore.Setup(s => s.LoadTreeAsync(project.FullPath, It.IsAny<CancellationToken>())).ReturnsAsync(persisted);
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        await tab.InitializeAsync();

        Assert.Equal("Characters", Assert.Single(tab.RootNode.Children).Name);
        Assert.Same(tab.RootNode, tab.SelectedNode);
        Assert.Same(tab.RootNode, Assert.Single(tab.RootNodeItems));
    }

    [Fact]
    public async Task RenameNodeAsyncAppliesTheEnteredNameFromTheDialog()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        tab.RootNode.AddChild(group);
        dialogService.Setup(s => s.ShowTextInputAsync(It.IsAny<string>(), It.IsAny<string>(), "Characters")).ReturnsAsync("Heroes");

        await tab.RenameNodeCommand.ExecuteAsync(group);

        Assert.Equal("Heroes", group.Name);
    }

    [Fact]
    public async Task RenameNodeAsyncIsANoOpForTheRootNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        await tab.RenameNodeCommand.ExecuteAsync(tab.RootNode);

        dialogService.Verify(s => s.ShowTextInputAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadReferenceFileAsyncAttachesTheFileToWhicheverNodeIsSelectedIncludingAnAssetNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.TextNodeViewModel textNode = new("t1", "Hero Bio");
        tab.RootNode.AddChild(textNode);
        tab.SelectedNode = textNode;
        dialogService.Setup(s => s.PickFileAsync(project.FullPath)).ReturnsAsync("/tmp/incoming/hero-pose.png");
        dataStore
            .Setup(s => s.StoreReferenceFileAsync(project.FullPath, "/tmp/incoming/hero-pose.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("references/hero-pose.png");

        await tab.UploadReferenceFileCommand.ExecuteAsync(null);

        Client.ViewModels.ReferenceFileViewModel file = Assert.Single(textNode.ReferenceFiles);
        Assert.Equal("hero-pose", file.Name);
        Assert.Equal(Client.Core.Models.ReferenceFileSource.Stored, file.Source);
        Assert.Equal("references/hero-pose.png", file.RelativePath);
    }

    [Fact]
    public async Task GenerateVariantsAsyncDelegatesToTheVariantGeneratorForTheSelectedAssetNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.TextNodeViewModel textNode = new("t1", "Hero Bio") { GenerateCount = 3 };
        tab.RootNode.AddChild(textNode);
        tab.SelectedNode = textNode;

        await tab.GenerateVariantsCommand.ExecuteAsync(null);

        variantGenerator.Verify(g => g.GenerateNewVariantsAsync(project.FullPath, textNode, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GenerateVariantsCommandCannotExecuteWhenAGroupNodeIsSelected()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        Assert.False(tab.GenerateVariantsCommand.CanExecute(null));
    }

    [Fact]
    public async Task RenameVariantAsyncAppliesTheEnteredNameFromTheDialog()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.TextVariantViewModel variant = new("v1", "Variant 1", Client.Core.Models.VariantStatus.Completed, DateTimeOffset.UtcNow);
        dialogService.Setup(s => s.ShowTextInputAsync(It.IsAny<string>(), It.IsAny<string>(), "Variant 1")).ReturnsAsync("Renamed Variant");

        await tab.RenameVariantCommand.ExecuteAsync(variant);

        Assert.Equal("Renamed Variant", variant.Name);
    }

    [Fact]
    public void DeleteVariantRemovesItFromTheSelectedAssetNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.TextNodeViewModel textNode = new("t1", "Hero Bio");
        Client.ViewModels.TextVariantViewModel variant = new("v1", "Variant 1", Client.Core.Models.VariantStatus.Completed, DateTimeOffset.UtcNow);
        textNode.Variants.Add(variant);
        tab.RootNode.AddChild(textNode);
        tab.SelectedNode = textNode;

        tab.DeleteVariantCommand.Execute(variant);

        Assert.Empty(textNode.Variants);
    }

    [Fact]
    public async Task AddLinkAsyncLinksTheSelectedNodeToTheNodePickedFromTheDialog()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel groupA = new("a", "A");
        Client.ViewModels.GroupNodeViewModel groupB = new("b", "B");
        tab.RootNode.AddChild(groupA);
        tab.RootNode.AddChild(groupB);
        tab.SelectedNode = groupA;
        dialogService
            .Setup(s => s.PickNodeAsync(It.IsAny<string>(), It.Is<IReadOnlyList<Client.ViewModels.ProjectNodeViewModel>>(list => list.Contains(groupB))))
            .ReturnsAsync(groupB);

        await tab.AddLinkCommand.ExecuteAsync(null);

        Assert.Same(groupB, Assert.Single(groupA.Links));
    }

    [Fact]
    public async Task PickOutputPathAsyncSetsAProjectRelativePathOnTheSelectedAssetNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.TextNodeViewModel textNode = new("t1", "Hero Bio");
        tab.RootNode.AddChild(textNode);
        tab.SelectedNode = textNode;
        dialogService.Setup(s => s.PickFolderAsync(project.FullPath)).ReturnsAsync(Path.Combine(project.FullPath, "Generated", "Hero"));

        await tab.PickOutputPathCommand.ExecuteAsync(null);

        Assert.Equal(Path.Combine("Generated", "Hero"), textNode.OutputPath);
    }

    [Fact]
    public async Task RefreshScenarioModelsAsyncWithNoCredentialsReportsAnError()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        await tab.RefreshScenarioModelsCommand.ExecuteAsync(null);

        modelCatalogService.Verify(
            s => s.GetTextToImageModelsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.NotNull(tab.ScenarioModelLoadError);
        Assert.Single(tab.AvailableScenarioModels);
    }

    [Fact]
    public async Task RefreshScenarioModelsAsyncWithCredentialsPopulatesModelsAfterTheInheritSentinel()
    {
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings
        {
            Api = new Client.Core.Models.ApiSettings { ScenarioApiKey = "key", ScenarioApiSecret = "secret" },
        });
        modelCatalogService.Setup(s => s.GetTextToImageModelsAsync("key", "secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" }]);
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        await tab.RefreshScenarioModelsCommand.ExecuteAsync(null);

        Assert.Equal(2, tab.AvailableScenarioModels.Count);
        Assert.Equal("", tab.AvailableScenarioModels[0].Id);
        Assert.Equal("model_a", tab.AvailableScenarioModels[1].Id);
        Assert.Null(tab.ScenarioModelLoadError);
    }

    [Fact]
    public async Task RefreshScenarioModelsAsyncPopulatesRootOptionsWithoutTheInheritSentinelAndDefaultsTheRootNode()
    {
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings
        {
            Api = new Client.Core.Models.ApiSettings { ScenarioApiKey = "key", ScenarioApiSecret = "secret" },
        });
        modelCatalogService.Setup(s => s.GetTextToImageModelsAsync("key", "secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" }, new Client.Core.Models.ScenarioModel { Id = "model_b", Name = "Model B" }]);
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();

        await tab.RefreshScenarioModelsCommand.ExecuteAsync(null);

        Assert.Equal(["model_a", "model_b"], tab.RootScenarioModelOptions.Select(model => model.Id));
        Assert.Equal("model_a", tab.RootNode.ScenarioModelId);
    }

    [Fact]
    public async Task RefreshScenarioModelsAsyncDoesNotOverrideAnAlreadyChosenRootModel()
    {
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings
        {
            Api = new Client.Core.Models.ApiSettings { ScenarioApiKey = "key", ScenarioApiSecret = "secret" },
        });
        modelCatalogService.Setup(s => s.GetTextToImageModelsAsync("key", "secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" }]);
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        tab.RootNode.ScenarioModelId = "model_existing";

        await tab.RefreshScenarioModelsCommand.ExecuteAsync(null);

        Assert.Equal("model_existing", tab.RootNode.ScenarioModelId);
    }

    [Fact]
    public async Task PickScenarioModelAsyncSetsTheNodesModelIdToThePickedResult()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters", "A group of heroic characters");
        tab.RootNode.AddChild(group);
        tab.AvailableScenarioModels.Add(new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" });
        modelPickerService
            .Setup(s => s.PickModelAsync(It.Is<string>(text => text.Contains("A group of heroic characters")), It.Is<IReadOnlyList<Client.Core.Models.ScenarioModel>>(list => list.Count == 1 && list[0].Id == "model_a"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("model_a");

        await tab.PickScenarioModelCommand.ExecuteAsync(group);

        Assert.Equal("model_a", group.ScenarioModelId);
    }

    [Fact]
    public async Task PickScenarioModelAsyncWorksForTheRootNode()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        tab.AvailableScenarioModels.Add(new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" });
        modelPickerService
            .Setup(s => s.PickModelAsync(It.IsAny<string>(), It.Is<IReadOnlyList<Client.Core.Models.ScenarioModel>>(list => list.Count == 1 && list[0].Id == "model_a"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("model_a");

        await tab.PickScenarioModelCommand.ExecuteAsync(tab.RootNode);

        Assert.Equal("model_a", tab.RootNode.ScenarioModelId);
    }

    [Fact]
    public async Task PickScenarioModelAsyncShowsAMessageWhenNoModelsAreAvailable()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        tab.RootNode.AddChild(group);

        await tab.PickScenarioModelCommand.ExecuteAsync(group);

        dialogService.Verify(s => s.ShowMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        modelPickerService.Verify(
            s => s.PickModelAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Client.Core.Models.ScenarioModel>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PickScenarioModelAsyncShowsAMessageWhenThePickerThrows()
    {
        Client.ViewModels.ProjectTabViewModel tab = CreateTab();
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        tab.RootNode.AddChild(group);
        tab.AvailableScenarioModels.Add(new Client.Core.Models.ScenarioModel { Id = "model_a", Name = "Model A" });
        modelPickerService
            .Setup(s => s.PickModelAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Client.Core.Models.ScenarioModel>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to start the local Claude Code CLI."));

        await tab.PickScenarioModelCommand.ExecuteAsync(group);

        dialogService.Verify(s => s.ShowMessageAsync("Cannot Pick Model", "Failed to start the local Claude Code CLI."), Times.Once);
        Assert.Equal("", group.ScenarioModelId);
    }
}
