namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// A node that organizes other nodes underneath it and represents a concept referenced when generating
/// assets nested under it. Holds free-form context text plus any reference files supporting that context.
/// </summary>
internal partial class GroupNodeViewModel : ProjectNodeViewModel
{
    /// <summary>
    /// Rebuilds an observable group node, and its full nested subtree, from its persisted form.
    /// </summary>
    public static GroupNodeViewModel FromModel(GroupNode model, string projectPath)
    {
        GroupNodeViewModel node = new(model.Id, model.Name, model.Context) { ScenarioModelId = model.ScenarioModelId };
        node.PopulateFrom(model, projectPath);
        return node;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupNodeViewModel"/> class.
    /// </summary>
    public GroupNodeViewModel(string id, string name, string context = "") : base(id, name, context)
    {
    }

    private readonly ObservableCollection<ProjectNodeViewModel> children = [];

    /// <inheritdoc />
    public override ObservableCollection<ProjectNodeViewModel> Children => children;

    /// <summary>
    /// Attaches <paramref name="child"/> under this node, wiring its <see cref="ProjectNodeViewModel.Changed"/>
    /// event to bubble up through this node's own <see cref="ProjectNodeViewModel.Changed"/> event.
    /// </summary>
    public void AddChild(ProjectNodeViewModel child)
    {
        child.Parent = this;
        child.Changed += RaiseChanged;
        children.Add(child);
        RaiseChanged();
    }

    /// <summary>
    /// Detaches <paramref name="child"/> from this node.
    /// </summary>
    public void RemoveChild(ProjectNodeViewModel child)
    {
        child.Changed -= RaiseChanged;
        children.Remove(child);
        child.Parent = null;
        RaiseChanged();
    }

    /// <inheritdoc />
    public override ProjectNode ToModel() => new GroupNode
    {
        Id = Id,
        Name = Name,
        Context = Context,
        ReferenceFiles = [.. ReferenceFiles.Select(file => file.ToModel())],
        LinkedNodeIds = [.. Links.Select(link => link.Id)],
        ScenarioModelId = ScenarioModelId,
        Children = [.. Children.Select(child => child.ToModel())],
    };

    /// <summary>
    /// Repopulates this node's <see cref="ProjectNodeViewModel.ReferenceFiles"/> and <see cref="Children"/>
    /// from <paramref name="model"/>, shared by <see cref="FromModel"/> and
    /// <see cref="RootNodeViewModel.FromModel"/>.
    /// </summary>
    protected void PopulateFrom(GroupNode model, string projectPath)
    {
        foreach (ReferenceFile file in model.ReferenceFiles)
        {
            AddReferenceFile(ReferenceFileViewModel.FromModel(file, projectPath));
        }

        foreach (ProjectNode child in model.Children)
        {
            AddChild(ProjectNodeViewModel.FromModel(child, projectPath));
        }
    }

    /// <summary>
    /// Creates and attaches a new child group node - backs the sidebar tree's right-click "Create Group"
    /// menu item, which binds straight to this node rather than through an ancestor lookup, since that
    /// lookup cannot reliably cross into a popup's own visual tree.
    /// </summary>
    [RelayCommand]
    private void CreateGroup() => AddChild(new GroupNodeViewModel(Guid.NewGuid().ToString("N"), "New Group"));

    /// <summary>
    /// Creates and attaches a new child text asset node - see <see cref="CreateGroup"/>.
    /// </summary>
    [RelayCommand]
    private void CreateText() => AddChild(new TextNodeViewModel(Guid.NewGuid().ToString("N"), "New Text"));

    /// <summary>
    /// Creates and attaches a new child image asset node - see <see cref="CreateGroup"/>.
    /// </summary>
    [RelayCommand]
    private void CreateImage() => AddChild(new ImageNodeViewModel(Guid.NewGuid().ToString("N"), "New Image"));

    /// <summary>
    /// Creates and attaches a new child mesh asset node - see <see cref="CreateGroup"/>.
    /// </summary>
    [RelayCommand]
    private void CreateMesh() => AddChild(new MeshNodeViewModel(Guid.NewGuid().ToString("N"), "New Mesh"));
}
