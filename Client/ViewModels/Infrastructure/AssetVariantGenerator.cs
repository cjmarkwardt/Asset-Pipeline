namespace Markwardt.AssetPipeline.Client.ViewModels.Infrastructure;

/// <summary>
/// Drives an <see cref="AssetNodeViewModel"/>'s variant generation pipeline: gathering context (see
/// <see cref="NodeContextAggregator"/>), calling the right generation service for the node's kind, and
/// updating variant state/output paths as generation progresses.
/// </summary>
internal interface IAssetVariantGenerator
{
    /// <summary>
    /// Creates and generates <paramref name="count"/> new variants on <paramref name="node"/>.
    /// </summary>
    Task GenerateNewVariantsAsync(string projectPath, AssetNodeViewModel node, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates an existing variant from scratch (for a <see cref="MeshVariantViewModel"/>, this
    /// regenerates both its concept image and its mesh).
    /// </summary>
    Task RegenerateAsync(string projectPath, AssetNodeViewModel node, AssetVariantViewModel variant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates only <paramref name="variant"/>'s mesh, keeping its existing concept image.
    /// </summary>
    Task RegenerateMeshOnlyAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves <paramref name="variant"/>'s concept image, continuing on to mesh generation.
    /// </summary>
    Task ApproveConceptAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IAssetVariantGenerator" />
internal sealed class AssetVariantGenerator(IAssetGenerationService generationService) : IAssetVariantGenerator
{
    /// <inheritdoc />
    public async Task GenerateNewVariantsAsync(string projectPath, AssetNodeViewModel node, int count, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            AssetVariantViewModel variant = CreateVariant(node);
            node.Variants.Add(variant);
            node.NotifyVariantsChanged();
            await GenerateAsync(projectPath, node, variant, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task RegenerateAsync(string projectPath, AssetNodeViewModel node, AssetVariantViewModel variant, CancellationToken cancellationToken = default) =>
        GenerateAsync(projectPath, node, variant, cancellationToken);

    /// <inheritdoc />
    public Task RegenerateMeshOnlyAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken = default) =>
        GenerateMeshStageAsync(projectPath, node, variant, cancellationToken);

    /// <inheritdoc />
    public Task ApproveConceptAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken = default) =>
        GenerateMeshStageAsync(projectPath, node, variant, cancellationToken);

    private static AssetVariantViewModel CreateVariant(AssetNodeViewModel node)
    {
        string id = Guid.NewGuid().ToString("N");
        string name = $"Variant {node.Variants.Count + 1}";
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        return node switch
        {
            TextNodeViewModel => new TextVariantViewModel(id, name, VariantStatus.Pending, createdAt),
            ImageNodeViewModel => new ImageVariantViewModel(id, name, VariantStatus.Pending, createdAt),
            MeshNodeViewModel => new MeshVariantViewModel(id, name, VariantStatus.Pending, createdAt),
            _ => throw new NotSupportedException($"Unsupported asset node type '{node.GetType()}'."),
        };
    }

    private Task GenerateAsync(string projectPath, AssetNodeViewModel node, AssetVariantViewModel variant, CancellationToken cancellationToken) => (node, variant) switch
    {
        (TextNodeViewModel textNode, TextVariantViewModel textVariant) => GenerateTextVariantAsync(projectPath, textNode, textVariant, cancellationToken),
        (ImageNodeViewModel imageNode, ImageVariantViewModel imageVariant) => GenerateImageVariantAsync(projectPath, imageNode, imageVariant, cancellationToken),
        (MeshNodeViewModel meshNode, MeshVariantViewModel meshVariant) => GenerateMeshConceptAsync(projectPath, meshNode, meshVariant, cancellationToken),
        _ => throw new NotSupportedException($"Unsupported node/variant combination '{node.GetType()}'/'{variant.GetType()}'."),
    };

    private async Task GenerateTextVariantAsync(string projectPath, TextNodeViewModel node, TextVariantViewModel variant, CancellationToken cancellationToken)
    {
        variant.Status = VariantStatus.Generating;
        variant.ErrorMessage = null;
        try
        {
            AggregatedContext context = NodeContextAggregator.Collect(node);
            TextGenerationRequest request = new()
            {
                Context = context.Text,
                Unit = node.Unit,
                MinUnits = node.MinUnits,
                MaxUnits = node.MaxUnits,
                IsJsonFormatted = node.IsJsonFormatted,
                JsonSchema = node.IsJsonFormatted ? node.JsonSchema : null,
            };
            string outputDirectory = AssetOutputPaths.Resolve(projectPath, node.OutputPath);
            variant.OutputFilePath = await generationService.GenerateTextAsync(outputDirectory, variant.Id, request, cancellationToken);
            variant.Status = VariantStatus.Completed;
        }
        catch (Exception ex)
        {
            variant.Status = VariantStatus.Failed;
            variant.ErrorMessage = ex.Message;
        }
        finally
        {
            node.NotifyVariantsChanged();
        }
    }

    private async Task GenerateImageVariantAsync(string projectPath, ImageNodeViewModel node, ImageVariantViewModel variant, CancellationToken cancellationToken)
    {
        variant.Status = VariantStatus.Generating;
        variant.ErrorMessage = null;
        try
        {
            AggregatedContext context = NodeContextAggregator.Collect(node);
            string outputDirectory = AssetOutputPaths.Resolve(projectPath, node.OutputPath);
            variant.OutputFilePath = await generationService.GenerateImageAsync(
                outputDirectory, variant.Id, BuildImagePrompt(node.Name, context), node.ResolveScenarioModelId(), cancellationToken);
            variant.Status = VariantStatus.Completed;
        }
        catch (Exception ex)
        {
            variant.Status = VariantStatus.Failed;
            variant.ErrorMessage = ex.Message;
        }
        finally
        {
            node.NotifyVariantsChanged();
        }
    }

    private async Task GenerateMeshConceptAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken)
    {
        variant.Status = VariantStatus.Generating;
        variant.ErrorMessage = null;
        try
        {
            AggregatedContext context = NodeContextAggregator.Collect(node);
            string outputDirectory = AssetOutputPaths.Resolve(projectPath, node.OutputPath);
            variant.ConceptImagePath = await generationService.GenerateImageAsync(
                outputDirectory, $"{variant.Id}-concept", BuildImagePrompt(node.Name, context), node.ResolveScenarioModelId(), cancellationToken);
            variant.AbsoluteConceptImagePath = Path.Combine(outputDirectory, variant.ConceptImagePath);

            if (node.RequireConceptApproval)
            {
                variant.Status = VariantStatus.AwaitingApproval;
                return;
            }

            await GenerateMeshStageCoreAsync(outputDirectory, variant, cancellationToken);
        }
        catch (Exception ex)
        {
            variant.Status = VariantStatus.Failed;
            variant.ErrorMessage = ex.Message;
        }
        finally
        {
            node.NotifyVariantsChanged();
        }
    }

    private async Task GenerateMeshStageAsync(string projectPath, MeshNodeViewModel node, MeshVariantViewModel variant, CancellationToken cancellationToken)
    {
        if (variant.ConceptImagePath is null)
        {
            variant.Status = VariantStatus.Failed;
            variant.ErrorMessage = "This variant has no concept image to generate a mesh from.";
            node.NotifyVariantsChanged();
            return;
        }

        try
        {
            string outputDirectory = AssetOutputPaths.Resolve(projectPath, node.OutputPath);
            await GenerateMeshStageCoreAsync(outputDirectory, variant, cancellationToken);
        }
        finally
        {
            node.NotifyVariantsChanged();
        }
    }

    private async Task GenerateMeshStageCoreAsync(string outputDirectory, MeshVariantViewModel variant, CancellationToken cancellationToken)
    {
        variant.Status = VariantStatus.Generating;
        variant.ErrorMessage = null;
        try
        {
            string conceptImagePath = Path.Combine(outputDirectory, variant.ConceptImagePath!);
            variant.OutputFilePath = await generationService.GenerateModelAsync(outputDirectory, variant.Id, conceptImagePath, cancellationToken);
            variant.Status = VariantStatus.Completed;
        }
        catch (Exception ex)
        {
            variant.Status = VariantStatus.Failed;
            variant.ErrorMessage = ex.Message;
        }
    }

    private static string BuildImagePrompt(string nodeName, AggregatedContext context) =>
        string.IsNullOrWhiteSpace(context.Text) ? nodeName : $"{nodeName}\n\n{context.Text}";
}
