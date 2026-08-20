namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// A node representing a specific asset to be generated, as opposed to a <see cref="GroupNodeViewModel"/>,
/// which only organizes other nodes. Shows the same context/reference-file editing as a group node, plus an
/// output path and a list of generated (or in-progress) asset variants.
/// </summary>
internal abstract partial class AssetNodeViewModel : ProjectNodeViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetNodeViewModel"/> class.
    /// </summary>
    protected AssetNodeViewModel(string id, string name, string context = "", string outputPath = "") : base(id, name, context) =>
        this.outputPath = outputPath;

    [ObservableProperty]
    private string outputPath;

    /// <summary>Transient (not persisted) count of new variants to generate the next time <c>Generate</c> is used.</summary>
    [ObservableProperty]
    private int generateCount = 1;

    /// <summary>
    /// Gets the short label identifying this node's asset kind, shown in the content area.
    /// </summary>
    public abstract string AssetKindLabel { get; }

    /// <summary>
    /// Gets this node's generated (and in-progress) asset variants.
    /// </summary>
    public ObservableCollection<AssetVariantViewModel> Variants { get; } = [];

    /// <summary>
    /// Notifies <see cref="ProjectNodeViewModel.Changed"/> that this node's <see cref="Variants"/> changed in
    /// a way that needs to be persisted - used by the asset generation pipeline, which mutates variants
    /// directly rather than through a command on this view model.
    /// </summary>
    public void NotifyVariantsChanged() => RaiseChanged();

    partial void OnOutputPathChanged(string value) => RaiseChanged();

    /// <summary>
    /// Populates this node's <see cref="ProjectNodeViewModel.ReferenceFiles"/> and <see cref="Variants"/>
    /// from <paramref name="model"/>, shared by every concrete asset node's own <c>FromModel</c>.
    /// </summary>
    protected void PopulateFrom(AssetNode model, string projectPath)
    {
        foreach (ReferenceFile file in model.ReferenceFiles)
        {
            AddReferenceFile(ReferenceFileViewModel.FromModel(file, projectPath));
        }

        string outputDirectory = AssetOutputPaths.Resolve(projectPath, model.OutputPath);
        foreach (AssetVariant variant in model.Variants)
        {
            Variants.Add(AssetVariantViewModel.FromModel(variant, outputDirectory));
        }
    }
}
