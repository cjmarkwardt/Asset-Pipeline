namespace Markwardt.AssetPipeline.Tests.ViewModels.Infrastructure;

/// <summary>
/// Tests for <see cref="Client.ViewModels.Infrastructure.NodeContextAggregator"/>.
/// </summary>
public sealed class NodeContextAggregatorTests
{
    [Fact]
    public void CollectIncludesTheNodeItselfAndItsFullAncestorChain()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project", "Root context");
        Client.ViewModels.GroupNodeViewModel group = new("g1", "Group", "Group context");
        Client.ViewModels.TextNodeViewModel text = new("t1", "Text", "Text context");
        root.AddChild(group);
        group.AddChild(text);

        Client.ViewModels.Infrastructure.AggregatedContext result = Client.ViewModels.Infrastructure.NodeContextAggregator.Collect(text);

        Assert.Contains("Root context", result.Text);
        Assert.Contains("Group context", result.Text);
        Assert.Contains("Text context", result.Text);
    }

    [Fact]
    public void CollectIncludesALinkedNodeAndItsOwnAncestorChain()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.GroupNodeViewModel lore = new("lore", "Lore", "World lore context");
        Client.ViewModels.GroupNodeViewModel characters = new("chars", "Characters");
        Client.ViewModels.TextNodeViewModel hero = new("hero", "Hero", "Hero context");
        root.AddChild(lore);
        root.AddChild(characters);
        characters.AddChild(hero);
        hero.AddLink(lore);

        Client.ViewModels.Infrastructure.AggregatedContext result = Client.ViewModels.Infrastructure.NodeContextAggregator.Collect(hero);

        Assert.Contains("World lore context", result.Text);
        Assert.Contains("Hero context", result.Text);
    }

    [Fact]
    public void CollectDoesNotDuplicateASharedAncestorReachableThroughBothTheParentChainAndALink()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project", "Shared root context");
        Client.ViewModels.GroupNodeViewModel branchA = new("a", "Branch A");
        Client.ViewModels.GroupNodeViewModel branchB = new("b", "Branch B");
        Client.ViewModels.TextNodeViewModel node = new("n1", "Node");
        root.AddChild(branchA);
        root.AddChild(branchB);
        branchA.AddChild(node);
        node.AddLink(branchB);

        Client.ViewModels.Infrastructure.AggregatedContext result = Client.ViewModels.Infrastructure.NodeContextAggregator.Collect(node);

        int occurrences = result.Text.Split("Shared root context").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void CollectGathersReferenceFilesFromEveryIncludedSource()
    {
        Client.ViewModels.RootNodeViewModel root = new("root", "Project");
        Client.ViewModels.TextNodeViewModel node = new("n1", "Node");
        root.AddChild(node);
        root.AddReferenceFile(new Client.ViewModels.ReferenceFileViewModel(
            "Style Guide", Client.Core.Models.ReferenceFileSource.ProjectPath, "art/style.png", "/tmp/project/art/style.png"));
        node.AddReferenceFile(new Client.ViewModels.ReferenceFileViewModel(
            "Hero Pose", Client.Core.Models.ReferenceFileSource.Stored, "references/pose.png", "/tmp/project/.astproj/references/pose.png"));

        Client.ViewModels.Infrastructure.AggregatedContext result = Client.ViewModels.Infrastructure.NodeContextAggregator.Collect(node);

        Assert.Contains(result.ReferenceFiles, file => file.Name == "Style Guide");
        Assert.Contains(result.ReferenceFiles, file => file.Name == "Hero Pose");
    }
}
