namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// An <see cref="AssetNodeViewModel"/> representing a text asset to be generated via a local Claude Code
/// instance.
/// </summary>
internal sealed partial class TextNodeViewModel : AssetNodeViewModel
{
    /// <summary>
    /// Rebuilds an observable text node from its persisted form.
    /// </summary>
    public static TextNodeViewModel FromModel(TextNode model, string projectPath)
    {
        TextNodeViewModel node = new(model.Id, model.Name, model.Context, model.OutputPath)
        {
            ScenarioModelId = model.ScenarioModelId,
            Unit = model.Unit,
            MinUnits = model.MinUnits,
            MaxUnits = model.MaxUnits,
            IsJsonFormatted = model.IsJsonFormatted,
            JsonSchema = model.JsonSchema,
        };
        node.PopulateFrom(model, projectPath);
        return node;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextNodeViewModel"/> class.
    /// </summary>
    public TextNodeViewModel(string id, string name, string context = "", string outputPath = "") : base(id, name, context, outputPath)
    {
    }

    [ObservableProperty]
    private TextUnit unit = TextUnit.Words;

    [ObservableProperty]
    private int minUnits = 50;

    [ObservableProperty]
    private int maxUnits = 200;

    [ObservableProperty]
    private bool isJsonFormatted;

    [ObservableProperty]
    private string jsonSchema = "";

    /// <inheritdoc />
    public override string AssetKindLabel => "Text";

    /// <summary>
    /// Gets the possible values for <see cref="Unit"/>, backing the settings dropdown.
    /// </summary>
    public IReadOnlyList<TextUnit> AvailableUnits { get; } = Enum.GetValues<TextUnit>();

    /// <inheritdoc />
    public override ProjectNode ToModel() => new TextNode
    {
        Id = Id,
        Name = Name,
        Context = Context,
        ReferenceFiles = [.. ReferenceFiles.Select(file => file.ToModel())],
        LinkedNodeIds = [.. Links.Select(link => link.Id)],
        ScenarioModelId = ScenarioModelId,
        OutputPath = OutputPath,
        Variants = [.. Variants.Select(variant => variant.ToModel())],
        Unit = Unit,
        MinUnits = MinUnits,
        MaxUnits = MaxUnits,
        IsJsonFormatted = IsJsonFormatted,
        JsonSchema = JsonSchema,
    };

    partial void OnUnitChanged(TextUnit value) => RaiseChanged();

    partial void OnMinUnitsChanged(int value) => RaiseChanged();

    partial void OnMaxUnitsChanged(int value) => RaiseChanged();

    partial void OnIsJsonFormattedChanged(bool value) => RaiseChanged();

    partial void OnJsonSchemaChanged(string value) => RaiseChanged();
}
