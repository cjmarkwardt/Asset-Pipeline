namespace Markwardt.AssetPipeline.Tests.Core.Services;

/// <summary>
/// Tests for <see cref="Client.Core.Services.ProjectDataStore"/>.
/// </summary>
public sealed class ProjectDataStoreTests : IDisposable
{
    private readonly string projectFolder = Path.Combine(Path.GetTempPath(), $"astproj-datastore-tests-{Guid.NewGuid():N}");

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(projectFolder))
        {
            Directory.Delete(projectFolder, recursive: true);
        }
    }

    [Fact]
    public async Task LoadTreeAsyncReturnsFreshRootWhenNoTreeHasBeenSaved()
    {
        Directory.CreateDirectory(projectFolder);
        Client.Core.Services.ProjectDataStore store = new();

        Client.Core.Models.ProjectNode root = await store.LoadTreeAsync(projectFolder);

        Assert.IsType<Client.Core.Models.RootNode>(root);
        Assert.Empty(((Client.Core.Models.GroupNode)root).Children);
    }

    [Fact]
    public async Task SaveTreeAsyncThenLoadTreeAsyncRoundTripsTheFullTree()
    {
        Directory.CreateDirectory(projectFolder);
        Client.Core.Services.ProjectDataStore store = new();
        Client.Core.Models.RootNode root = new()
        {
            Id = "root",
            Name = "Project",
            Context = "A test project",
            ReferenceFiles = [new Client.Core.Models.ReferenceFile { Name = "Style Guide", Source = Client.Core.Models.ReferenceFileSource.ProjectPath, Path = "art/style.png" }],
            Children =
            [
                new Client.Core.Models.GroupNode { Id = "g1", Name = "Characters" },
                new Client.Core.Models.TextNode { Id = "t1", Name = "Intro Blurb" },
            ],
        };

        await store.SaveTreeAsync(projectFolder, root);
        Client.Core.Models.ProjectNode loaded = await store.LoadTreeAsync(projectFolder);

        Client.Core.Models.RootNode loadedRoot = Assert.IsType<Client.Core.Models.RootNode>(loaded);
        Assert.Equal(root.Context, loadedRoot.Context);
        Assert.Equal("Style Guide", Assert.Single(loadedRoot.ReferenceFiles).Name);
        Assert.Equal(2, loadedRoot.Children.Count);
        Assert.IsType<Client.Core.Models.GroupNode>(loadedRoot.Children[0]);
        Assert.IsType<Client.Core.Models.TextNode>(loadedRoot.Children[1]);
    }

    [Fact]
    public async Task SaveTreeAsyncCreatesTheProjectDataFolderIfMissing()
    {
        Client.Core.Services.ProjectDataStore store = new();
        Client.Core.Models.ProjectNode root = new Client.Core.Models.RootNode { Id = "root", Name = "Project" };

        await store.SaveTreeAsync(projectFolder, root);

        Assert.True(Directory.Exists(Path.Combine(projectFolder, Client.Core.Models.ProjectInfo.DataFolderName)));
    }

    [Fact]
    public async Task StoreReferenceFileAsyncCopiesTheFileIntoTheReferencesFolder()
    {
        Directory.CreateDirectory(projectFolder);
        string sourceFile = Path.Combine(projectFolder, "incoming.png");
        await File.WriteAllBytesAsync(sourceFile, [1, 2, 3]);
        Client.Core.Services.ProjectDataStore store = new();

        string relativePath = await store.StoreReferenceFileAsync(projectFolder, sourceFile);

        string expectedPath = Path.Combine(projectFolder, Client.Core.Models.ProjectInfo.DataFolderName, relativePath);
        Assert.True(File.Exists(expectedPath));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(expectedPath));
    }

    [Fact]
    public async Task StoreReferenceFileAsyncDedupesAConflictingFileName()
    {
        Directory.CreateDirectory(projectFolder);
        string firstSource = Path.Combine(projectFolder, "incoming.png");
        string secondSource = Path.Combine(projectFolder, "other", "incoming.png");
        Directory.CreateDirectory(Path.GetDirectoryName(secondSource)!);
        await File.WriteAllBytesAsync(firstSource, [1]);
        await File.WriteAllBytesAsync(secondSource, [2]);
        Client.Core.Services.ProjectDataStore store = new();

        string firstRelativePath = await store.StoreReferenceFileAsync(projectFolder, firstSource);
        string secondRelativePath = await store.StoreReferenceFileAsync(projectFolder, secondSource);

        Assert.NotEqual(firstRelativePath, secondRelativePath);
    }
}
