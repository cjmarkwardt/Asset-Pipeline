namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Base view model for a single node in a project's node tree, editable in-place and observable so the
/// sidebar tree and the content area stay in sync. Every node is either a <see cref="GroupNodeViewModel"/>
/// or an <see cref="AssetNodeViewModel"/>, or one of their derived types.
/// </summary>
internal abstract partial class ProjectNodeViewModel : ViewModelBase
{
    /// <summary>
    /// Rebuilds an observable node (and, for a <see cref="GroupNodeViewModel"/>, its full nested subtree)
    /// from its persisted form. Any links it holds are only queued (see <see cref="PendingLinkIds"/>) - call
    /// <see cref="ResolveLinks"/> once the full tree has been rebuilt to actually wire them up.
    /// </summary>
    public static ProjectNodeViewModel FromModel(ProjectNode model, string projectPath)
    {
        ProjectNodeViewModel node = model switch
        {
            RootNode root => RootNodeViewModel.FromModel(root, projectPath),
            GroupNode group => GroupNodeViewModel.FromModel(group, projectPath),
            TextNode text => TextNodeViewModel.FromModel(text, projectPath),
            ImageNode image => ImageNodeViewModel.FromModel(image, projectPath),
            MeshNode mesh => MeshNodeViewModel.FromModel(mesh, projectPath),
            _ => throw new NotSupportedException($"Unsupported project node model type '{model.GetType()}'."),
        };

        node.PendingLinkIds.AddRange(model.LinkedNodeIds);
        return node;
    }

    /// <summary>
    /// Resolves every node's <see cref="PendingLinkIds"/> (queued during <see cref="FromModel"/>) into actual
    /// <see cref="Links"/> references, once <paramref name="root"/>'s full tree has been rebuilt and every
    /// node's id is known.
    /// </summary>
    public static void ResolveLinks(ProjectNodeViewModel root)
    {
        Dictionary<string, ProjectNodeViewModel> nodesById = [];
        void Index(ProjectNodeViewModel node)
        {
            nodesById[node.Id] = node;
            foreach (ProjectNodeViewModel child in node.Children)
            {
                Index(child);
            }
        }

        Index(root);

        void Resolve(ProjectNodeViewModel node)
        {
            foreach (string linkedId in node.PendingLinkIds)
            {
                if (nodesById.TryGetValue(linkedId, out ProjectNodeViewModel? target) && !ReferenceEquals(target, node))
                {
                    node.links.Add(target);
                }
            }

            node.PendingLinkIds.Clear();
            foreach (ProjectNodeViewModel child in node.Children)
            {
                Resolve(child);
            }
        }

        Resolve(root);
    }

    private readonly ObservableCollection<ProjectNodeViewModel> noChildren = [];
    private readonly ObservableCollection<ProjectNodeViewModel> links = [];

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string context;

    [ObservableProperty]
    private string scenarioModelId = "";

    /// <summary>Drives the sidebar tree's expand/collapse state, kept per-node so it survives resorting or a parent re-selection.</summary>
    [ObservableProperty]
    private bool isExpanded = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNodeViewModel"/> class.
    /// </summary>
    protected ProjectNodeViewModel(string id, string name, string context = "")
    {
        Id = id;
        this.name = name;
        this.context = context;
        Links = new(links);
    }

    /// <summary>
    /// Raised whenever this node, or any node in its subtree, changes in a way that needs to be persisted.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets this node's stable identifier, unique within its project.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the parent this node is currently attached to, or <see langword="null"/> for the root node.
    /// </summary>
    public ProjectNodeViewModel? Parent { get; internal set; }

    /// <summary>
    /// Gets this node's child nodes, always empty except for a <see cref="GroupNodeViewModel"/> (which
    /// overrides this to expose its real, mutable child collection). Declared here, rather than only on
    /// <see cref="GroupNodeViewModel"/>, so the sidebar tree's single item template can bind to it
    /// regardless of which kind of node a given row is showing.
    /// </summary>
    public virtual ObservableCollection<ProjectNodeViewModel> Children => noChildren;

    /// <summary>
    /// Gets the reference files supporting this node's <see cref="Context"/>.
    /// </summary>
    public ObservableCollection<ReferenceFileViewModel> ReferenceFiles { get; } = [];

    /// <summary>
    /// Gets the other nodes this node links to outside its own hierarchy (never a child/descendant or a
    /// parent/ancestor of this node - see <see cref="CanLinkTo"/>). A linked node's own context, reference
    /// files, and full ancestor chain are included when generating this node's (or a descendant asset
    /// node's) variants.
    /// </summary>
    public ReadOnlyObservableCollection<ProjectNodeViewModel> Links { get; }

    /// <summary>
    /// Gets the linked node ids queued by <see cref="FromModel"/>, not yet resolved into <see cref="Links"/>
    /// because the rest of the tree may not exist yet - resolved in a second pass by <see cref="ResolveLinks"/>.
    /// </summary>
    internal List<string> PendingLinkIds { get; } = [];

    /// <summary>
    /// Snapshots this node (and, for a <see cref="GroupNodeViewModel"/>, its full nested tree of children)
    /// into its persisted form.
    /// </summary>
    public abstract ProjectNode ToModel();

    /// <summary>
    /// Gets a value indicating whether this node could be linked to <paramref name="candidate"/>: not
    /// itself, not already linked, and not one of its own children/descendants or parents/ancestors.
    /// </summary>
    public bool CanLinkTo(ProjectNodeViewModel candidate)
    {
        if (ReferenceEquals(candidate, this) || links.Contains(candidate))
        {
            return false;
        }

        for (ProjectNodeViewModel? ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, candidate))
            {
                return false;
            }
        }

        for (ProjectNodeViewModel? ancestor = candidate.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, this))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a link from this node to <paramref name="target"/>. A no-op if <see cref="CanLinkTo"/> would
    /// return <see langword="false"/> for it.
    /// </summary>
    public void AddLink(ProjectNodeViewModel target)
    {
        if (!CanLinkTo(target))
        {
            return;
        }

        links.Add(target);
        RaiseChanged();
    }

    /// <summary>
    /// Resolves the scenario.com generation model to use for this node's own image generation (or, for a
    /// <see cref="MeshNodeViewModel"/>, its concept image generation): this node's own
    /// <see cref="ScenarioModelId"/> if set, otherwise the nearest ancestor's, walking up to the root.
    /// </summary>
    /// <returns>The resolved model id, or <see langword="null"/> if none of them has one set.</returns>
    public string? ResolveScenarioModelId()
    {
        for (ProjectNodeViewModel? current = this; current is not null; current = current.Parent)
        {
            if (!string.IsNullOrWhiteSpace(current.ScenarioModelId))
            {
                return current.ScenarioModelId;
            }
        }

        return null;
    }

    /// <summary>
    /// Attaches <paramref name="file"/> to this node, wiring its <see cref="ReferenceFileViewModel.Changed"/>
    /// event to bubble up through this node's own <see cref="Changed"/> event.
    /// </summary>
    public void AddReferenceFile(ReferenceFileViewModel file)
    {
        file.Changed += RaiseChanged;
        ReferenceFiles.Add(file);
        RaiseChanged();
    }

    partial void OnNameChanged(string value) => RaiseChanged();

    partial void OnContextChanged(string value) => RaiseChanged();

    partial void OnScenarioModelIdChanged(string value) => RaiseChanged();

    /// <summary>
    /// Raises <see cref="Changed"/>, bubbling up to this node's own parent (if any) via the wiring set up in
    /// <see cref="GroupNodeViewModel.AddChild"/>.
    /// </summary>
    protected void RaiseChanged() => Changed?.Invoke();

    [RelayCommand]
    private void RemoveReferenceFile(ReferenceFileViewModel file)
    {
        file.Changed -= RaiseChanged;
        ReferenceFiles.Remove(file);
        RaiseChanged();
    }

    [RelayCommand]
    private void RemoveLink(ProjectNodeViewModel target)
    {
        if (links.Remove(target))
        {
            RaiseChanged();
        }
    }
}
