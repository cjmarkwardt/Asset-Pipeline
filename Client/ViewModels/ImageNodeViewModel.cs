namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// An <see cref="AssetNodeViewModel"/> representing an image asset to be generated via scenario.com.
/// </summary>
internal sealed class ImageNodeViewModel : AssetNodeViewModel
{
    /// <summary>
    /// Rebuilds an observable image node from its persisted form.
    /// </summary>
    public static ImageNodeViewModel FromModel(ImageNode model, string projectPath)
    {
        ImageNodeViewModel node = new(model.Id, model.Name, model.Context, model.OutputPath) { ScenarioModelId = model.ScenarioModelId };
        node.PopulateFrom(model, projectPath);
        return node;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageNodeViewModel"/> class.
    /// </summary>
    public ImageNodeViewModel(string id, string name, string context = "", string outputPath = "") : base(id, name, context, outputPath)
    {
    }

    /// <inheritdoc />
    public override string AssetKindLabel => "Image";

    /// <inheritdoc />
    public override ProjectNode ToModel() => new ImageNode
    {
        Id = Id,
        Name = Name,
        Context = Context,
        ReferenceFiles = [.. ReferenceFiles.Select(file => file.ToModel())],
        LinkedNodeIds = [.. Links.Select(link => link.Id)],
        ScenarioModelId = ScenarioModelId,
        OutputPath = OutputPath,
        Variants = [.. Variants.Select(variant => variant.ToModel())],
    };
}
