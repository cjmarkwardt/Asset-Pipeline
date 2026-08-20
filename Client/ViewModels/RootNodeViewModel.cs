namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// The single, undeletable node every project tab's tree starts with. Behaves exactly like a
/// <see cref="GroupNodeViewModel"/> everywhere in the UI, aside from being the one node that can never be
/// deleted (enforced by <see cref="Parent"/> always staying <see langword="null"/>).
/// </summary>
internal sealed partial class RootNodeViewModel : GroupNodeViewModel
{
    /// <summary>
    /// Rebuilds an observable root node, and its full nested subtree, from its persisted form.
    /// </summary>
    public static RootNodeViewModel FromModel(RootNode model, string projectPath)
    {
        RootNodeViewModel node = new(model.Id, model.Name, model.Context) { ScenarioModelId = model.ScenarioModelId };
        node.PopulateFrom(model, projectPath);
        return node;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RootNodeViewModel"/> class.
    /// </summary>
    public RootNodeViewModel(string id, string name, string context = "") : base(id, name, context)
    {
    }

    /// <inheritdoc />
    public override ProjectNode ToModel() => new RootNode
    {
        Id = Id,
        Name = Name,
        Context = Context,
        ReferenceFiles = [.. ReferenceFiles.Select(file => file.ToModel())],
        LinkedNodeIds = [.. Links.Select(link => link.Id)],
        ScenarioModelId = ScenarioModelId,
        Children = [.. Children.Select(child => child.ToModel())],
    };
}
