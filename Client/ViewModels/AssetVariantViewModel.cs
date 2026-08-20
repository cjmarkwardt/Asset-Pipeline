namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// Base view model for a single generated (or in-progress) variant of an <see cref="AssetNodeViewModel"/>'s
/// asset, editable in-place and observable so the content area's variant list stays in sync. Every variant
/// is a <see cref="TextVariantViewModel"/>, <see cref="ImageVariantViewModel"/>, or
/// <see cref="MeshVariantViewModel"/>, matching the kind of its owning node.
/// </summary>
internal abstract partial class AssetVariantViewModel : ViewModelBase
{
    /// <summary>
    /// Rebuilds an observable variant from its persisted form.
    /// </summary>
    /// <param name="model">The persisted variant to rebuild.</param>
    /// <param name="outputDirectory">The owning node's full, resolved output folder path, used to resolve a
    /// <see cref="MeshVariantViewModel"/>'s concept image preview.</param>
    public static AssetVariantViewModel FromModel(AssetVariant model, string outputDirectory) => model switch
    {
        TextVariant text => new TextVariantViewModel(text.Id, text.Name, text.Status, text.CreatedAt, text.ErrorMessage) { OutputFilePath = text.OutputFilePath },
        ImageVariant image => new ImageVariantViewModel(image.Id, image.Name, image.Status, image.CreatedAt, image.ErrorMessage) { OutputFilePath = image.OutputFilePath },
        MeshVariant mesh => new MeshVariantViewModel(mesh.Id, mesh.Name, mesh.Status, mesh.CreatedAt, mesh.ErrorMessage)
        {
            ConceptImagePath = mesh.ConceptImagePath,
            OutputFilePath = mesh.OutputFilePath,
            AbsoluteConceptImagePath = mesh.ConceptImagePath is null ? null : Path.Combine(outputDirectory, mesh.ConceptImagePath),
        },
        _ => throw new NotSupportedException($"Unsupported asset variant model type '{model.GetType()}'."),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetVariantViewModel"/> class.
    /// </summary>
    protected AssetVariantViewModel(string id, string name, VariantStatus status, DateTimeOffset createdAt, string? errorMessage = null)
    {
        Id = id;
        this.name = name;
        this.status = status;
        CreatedAt = createdAt;
        this.errorMessage = errorMessage;
    }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private VariantStatus status;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isExpanded;

    /// <summary>
    /// Raised whenever this variant's editable state changes in a way that needs to be persisted.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets this variant's stable identifier, unique within its owning node.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the moment this variant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Snapshots this variant into its persisted form.
    /// </summary>
    public abstract AssetVariant ToModel();

    /// <summary>
    /// Raises <see cref="Changed"/>.
    /// </summary>
    protected void RaiseChanged() => Changed?.Invoke();

    partial void OnNameChanged(string value) => RaiseChanged();

    partial void OnStatusChanged(VariantStatus value) => RaiseChanged();

    partial void OnErrorMessageChanged(string? value) => RaiseChanged();
}
