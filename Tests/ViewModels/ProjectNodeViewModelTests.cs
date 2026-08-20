namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="Client.ViewModels.ProjectNodeViewModel"/>, covering behavior shared across every
/// concrete node kind.
/// </summary>
public sealed class ProjectNodeViewModelTests
{
    [Fact]
    public void AnAssetNodeAlwaysHasEmptyChildren()
    {
        Client.ViewModels.TextNodeViewModel text = new("t1", "Some Text");

        Assert.Empty(text.Children);
    }

    [Fact]
    public void FromModelBuildsTheMatchingViewModelKindForEveryNodeKind()
    {
        AssertFromModelKind(new Client.Core.Models.RootNode { Id = "n1", Name = "Node" }, typeof(Client.ViewModels.RootNodeViewModel));
        AssertFromModelKind(new Client.Core.Models.GroupNode { Id = "n1", Name = "Node" }, typeof(Client.ViewModels.GroupNodeViewModel));
        AssertFromModelKind(new Client.Core.Models.TextNode { Id = "n1", Name = "Node" }, typeof(Client.ViewModels.TextNodeViewModel));
        AssertFromModelKind(new Client.Core.Models.ImageNode { Id = "n1", Name = "Node" }, typeof(Client.ViewModels.ImageNodeViewModel));
        AssertFromModelKind(new Client.Core.Models.MeshNode { Id = "n1", Name = "Node" }, typeof(Client.ViewModels.MeshNodeViewModel));
    }

    private static void AssertFromModelKind(Client.Core.Models.ProjectNode model, Type expectedViewModelType)
    {
        Client.ViewModels.ProjectNodeViewModel viewModel = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");

        Assert.IsType(expectedViewModelType, viewModel);
        Assert.Equal("Node", viewModel.Name);
    }

    [Fact]
    public void AddLinkAttachesTheTargetAndRaisesChanged()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");
        Client.ViewModels.GroupNodeViewModel b = new("b", "B");
        root.AddChild(a);
        root.AddChild(b);
        int changedCount = 0;
        a.Changed += () => changedCount++;

        a.AddLink(b);

        Assert.Same(b, Assert.Single(a.Links));
        Assert.True(changedCount > 0);
    }

    [Fact]
    public void RemoveLinkCommandDetachesTheTarget()
    {
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");
        Client.ViewModels.GroupNodeViewModel b = new("b", "B");
        a.AddLink(b);

        a.RemoveLinkCommand.Execute(b);

        Assert.Empty(a.Links);
    }

    [Fact]
    public void CanLinkToIsFalseForSelf()
    {
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");

        Assert.False(a.CanLinkTo(a));
    }

    [Fact]
    public void CanLinkToIsFalseForAnAncestor()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel child = new("g1", "Child");
        root.AddChild(child);

        Assert.False(child.CanLinkTo(root));
    }

    [Fact]
    public void CanLinkToIsFalseForADescendant()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel child = new("g1", "Child");
        root.AddChild(child);

        Assert.False(root.CanLinkTo(child));
    }

    [Fact]
    public void CanLinkToIsFalseWhenAlreadyLinked()
    {
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");
        Client.ViewModels.GroupNodeViewModel b = new("b", "B");
        a.AddLink(b);

        Assert.False(a.CanLinkTo(b));
    }

    [Fact]
    public void CanLinkToIsTrueForAnUnrelatedNode()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");
        Client.ViewModels.GroupNodeViewModel b = new("b", "B");
        root.AddChild(a);
        root.AddChild(b);

        Assert.True(a.CanLinkTo(b));
    }

    [Fact]
    public void ResolveLinksWiresUpLinksAcrossTheWholeTreeAfterLoading()
    {
        Client.Core.Models.RootNode model = new()
        {
            Id = "root",
            Name = "Project",
            Children =
            [
                new Client.Core.Models.GroupNode { Id = "a", Name = "A", LinkedNodeIds = ["b"] },
                new Client.Core.Models.GroupNode { Id = "b", Name = "B" },
            ],
        };

        Client.ViewModels.ProjectNodeViewModel root = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");
        Client.ViewModels.ProjectNodeViewModel.ResolveLinks(root);

        Client.ViewModels.ProjectNodeViewModel a = root.Children[0];
        Client.ViewModels.ProjectNodeViewModel b = root.Children[1];
        Assert.Same(b, Assert.Single(a.Links));
    }

    [Fact]
    public void ToModelThenFromModelRoundTripsLinks()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel a = new("a", "A");
        Client.ViewModels.GroupNodeViewModel b = new("b", "B");
        root.AddChild(a);
        root.AddChild(b);
        a.AddLink(b);

        Client.Core.Models.ProjectNode model = root.ToModel();
        Client.ViewModels.ProjectNodeViewModel rebuilt = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");
        Client.ViewModels.ProjectNodeViewModel.ResolveLinks(rebuilt);

        Client.ViewModels.ProjectNodeViewModel rebuiltA = rebuilt.Children[0];
        Client.ViewModels.ProjectNodeViewModel rebuiltB = rebuilt.Children[1];
        Assert.Same(rebuiltB, Assert.Single(rebuiltA.Links));
    }
}
