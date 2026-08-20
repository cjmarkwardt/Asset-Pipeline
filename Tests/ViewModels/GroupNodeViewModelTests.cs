namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="Client.ViewModels.GroupNodeViewModel"/> and, since it behaves identically, its
/// <see cref="Client.ViewModels.RootNodeViewModel"/> subtype.
/// </summary>
public sealed class GroupNodeViewModelTests
{
    [Fact]
    public void AddChildSetsParentAndAddsToChildren()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel child = new("g1", "Characters");

        root.AddChild(child);

        Assert.Same(root, child.Parent);
        Assert.Same(child, Assert.Single(root.Children));
    }

    [Fact]
    public void RemoveChildClearsParentAndRemovesFromChildren()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel child = new("g1", "Characters");
        root.AddChild(child);

        root.RemoveChild(child);

        Assert.Null(child.Parent);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void ChangedOnADescendantBubblesUpThroughEveryAncestor()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        Client.ViewModels.GroupNodeViewModel subGroup = new("g2", "Heroes");
        root.AddChild(group);
        group.AddChild(subGroup);
        int rootChangedCount = 0;
        root.Changed += () => rootChangedCount++;

        subGroup.Name = "Villains";

        Assert.True(rootChangedCount > 0);
    }

    [Fact]
    public void ChangedOnAReferenceFileBubblesUpToTheOwningGroup()
    {
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        Client.ViewModels.ReferenceFileViewModel file = new("Hero Pose", Client.Core.Models.ReferenceFileSource.Stored, "references/pose.png", "/tmp/project/.astproj/references/pose.png");
        group.AddReferenceFile(file);
        int groupChangedCount = 0;
        group.Changed += () => groupChangedCount++;

        file.Name = "Hero Pose (Front)";

        Assert.True(groupChangedCount > 0);
    }

    [Fact]
    public void RemoveReferenceFileCommandRemovesTheFile()
    {
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        Client.ViewModels.ReferenceFileViewModel file = new("Hero Pose", Client.Core.Models.ReferenceFileSource.Stored, "references/pose.png", "/tmp/project/.astproj/references/pose.png");
        group.AddReferenceFile(file);

        group.RemoveReferenceFileCommand.Execute(file);

        Assert.Empty(group.ReferenceFiles);
    }

    [Theory]
    [InlineData(typeof(Client.ViewModels.GroupNodeViewModel))]
    [InlineData(typeof(Client.ViewModels.TextNodeViewModel))]
    [InlineData(typeof(Client.ViewModels.ImageNodeViewModel))]
    [InlineData(typeof(Client.ViewModels.MeshNodeViewModel))]
    public void CreateChildCommandsAddTheMatchingNodeKind(Type expectedType)
    {
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");

        if (expectedType == typeof(Client.ViewModels.GroupNodeViewModel))
        {
            group.CreateGroupCommand.Execute(null);
        }
        else if (expectedType == typeof(Client.ViewModels.TextNodeViewModel))
        {
            group.CreateTextCommand.Execute(null);
        }
        else if (expectedType == typeof(Client.ViewModels.ImageNodeViewModel))
        {
            group.CreateImageCommand.Execute(null);
        }
        else
        {
            group.CreateMeshCommand.Execute(null);
        }

        Assert.IsType(expectedType, Assert.Single(group.Children));
    }

    [Fact]
    public void ToModelThenFromModelRoundTripsContextReferenceFilesAndChildren()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project", "A global concept");
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Characters");
        root.AddChild(group);
        root.AddReferenceFile(new Client.ViewModels.ReferenceFileViewModel(
            "Style Guide", Client.Core.Models.ReferenceFileSource.ProjectPath, "art/style-guide.png", "/tmp/project/art/style-guide.png"));

        Client.Core.Models.ProjectNode model = root.ToModel();
        Client.ViewModels.ProjectNodeViewModel rebuilt = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");

        Client.ViewModels.RootNodeViewModel rebuiltRoot = Assert.IsType<Client.ViewModels.RootNodeViewModel>(rebuilt);
        Assert.Equal("A global concept", rebuiltRoot.Context);
        Assert.Equal("Style Guide", Assert.Single(rebuiltRoot.ReferenceFiles).Name);
        Assert.Equal("Characters", Assert.Single(rebuiltRoot.Children).Name);
        Assert.Same(rebuiltRoot, Assert.Single(rebuiltRoot.Children).Parent);
    }
}
