namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// An <see cref="AssetNodeViewModel"/> representing a 3D mesh asset to be generated: a concept image is
/// first generated via scenario.com, then a mesh is generated from that concept image via meshy.ai.
/// </summary>
internal sealed partial class MeshNodeViewModel : AssetNodeViewModel
{
    /// <summary>
    /// Rebuilds an observable mesh node from its persisted form.
    /// </summary>
    public static MeshNodeViewModel FromModel(MeshNode model, string projectPath)
    {
        MeshNodeViewModel node = new(model.Id, model.Name, model.Context, model.OutputPath)
        {
            ScenarioModelId = model.ScenarioModelId,
            RequireConceptApproval = model.RequireConceptApproval,
        };
        node.PopulateFrom(model, projectPath);
        return node;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshNodeViewModel"/> class.
    /// </summary>
    public MeshNodeViewModel(string id, string name, string context = "", string outputPath = "") : base(id, name, context, outputPath)
    {
    }

    [ObservableProperty]
    private bool requireConceptApproval;

    /// <inheritdoc />
    public override string AssetKindLabel => "Mesh";

    /// <inheritdoc />
    public override ProjectNode ToModel() => new MeshNode
    {
        Id = Id,
        Name = Name,
        Context = Context,
        ReferenceFiles = [.. ReferenceFiles.Select(file => file.ToModel())],
        LinkedNodeIds = [.. Links.Select(link => link.Id)],
        ScenarioModelId = ScenarioModelId,
        OutputPath = OutputPath,
        Variants = [.. Variants.Select(variant => variant.ToModel())],
        RequireConceptApproval = RequireConceptApproval,
    };

    partial void OnRequireConceptApprovalChanged(bool value) => RaiseChanged();
}
