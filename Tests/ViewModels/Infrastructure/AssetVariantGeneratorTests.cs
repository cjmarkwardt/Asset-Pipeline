namespace Markwardt.AssetPipeline.Tests.ViewModels.Infrastructure;

/// <summary>
/// Tests for <see cref="Client.ViewModels.Infrastructure.AssetVariantGenerator"/>.
/// </summary>
public sealed class AssetVariantGeneratorTests
{
    private readonly Mock<Client.Core.Services.IAssetGenerationService> generationService = new();
    private readonly Client.ViewModels.Infrastructure.AssetVariantGenerator generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetVariantGeneratorTests"/> class.
    /// </summary>
    public AssetVariantGeneratorTests() =>
        generator = new Client.ViewModels.Infrastructure.AssetVariantGenerator(generationService.Object);

    [Fact]
    public async Task GenerateNewVariantsAsyncOnATextNodeCreatesAndCompletesTheRequestedCount()
    {
        generationService
            .Setup(s => s.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Client.Core.Services.TextGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("variant.txt");
        Client.ViewModels.TextNodeViewModel node = new("t1", "Hero Bio");

        await generator.GenerateNewVariantsAsync("/tmp/project", node, 2);

        Assert.Equal(2, node.Variants.Count);
        Assert.All(node.Variants, variant => Assert.Equal(Client.Core.Models.VariantStatus.Completed, variant.Status));
        Assert.All(node.Variants, variant => Assert.Equal("variant.txt", ((Client.ViewModels.TextVariantViewModel)variant).OutputFilePath));
    }

    [Fact]
    public async Task GenerateNewVariantsAsyncMarksTheVariantFailedWhenGenerationThrows()
    {
        generationService
            .Setup(s => s.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Client.Core.Services.TextGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        Client.ViewModels.TextNodeViewModel node = new("t1", "Hero Bio");

        await generator.GenerateNewVariantsAsync("/tmp/project", node, 1);

        Client.ViewModels.AssetVariantViewModel variant = Assert.Single(node.Variants);
        Assert.Equal(Client.Core.Models.VariantStatus.Failed, variant.Status);
        Assert.Equal("boom", variant.ErrorMessage);
    }

    [Fact]
    public async Task GenerateNewVariantsAsyncOnAnImageNodeCallsImageGeneration()
    {
        generationService
            .Setup(s => s.GenerateImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("variant.png");
        Client.ViewModels.ImageNodeViewModel node = new("i1", "Hero Portrait");

        await generator.GenerateNewVariantsAsync("/tmp/project", node, 1);

        Client.ViewModels.ImageVariantViewModel variant = Assert.IsType<Client.ViewModels.ImageVariantViewModel>(Assert.Single(node.Variants));
        Assert.Equal(Client.Core.Models.VariantStatus.Completed, variant.Status);
        Assert.Equal("variant.png", variant.OutputFilePath);
    }

    [Fact]
    public async Task GenerateNewVariantsAsyncOnAMeshNodeWithoutApprovalGeneratesConceptThenMesh()
    {
        generationService
            .Setup(s => s.GenerateImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("concept.png");
        generationService
            .Setup(s => s.GenerateModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("model.glb");
        Client.ViewModels.MeshNodeViewModel node = new("m1", "Hero Statue") { RequireConceptApproval = false };

        await generator.GenerateNewVariantsAsync("/tmp/project", node, 1);

        Client.ViewModels.MeshVariantViewModel variant = Assert.IsType<Client.ViewModels.MeshVariantViewModel>(Assert.Single(node.Variants));
        Assert.Equal(Client.Core.Models.VariantStatus.Completed, variant.Status);
        Assert.Equal("concept.png", variant.ConceptImagePath);
        Assert.Equal("model.glb", variant.OutputFilePath);
        generationService.Verify(s => s.GenerateModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateNewVariantsAsyncOnAMeshNodeWithApprovalStopsAfterTheConceptImage()
    {
        generationService
            .Setup(s => s.GenerateImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("concept.png");
        Client.ViewModels.MeshNodeViewModel node = new("m1", "Hero Statue") { RequireConceptApproval = true };

        await generator.GenerateNewVariantsAsync("/tmp/project", node, 1);

        Client.ViewModels.MeshVariantViewModel variant = Assert.IsType<Client.ViewModels.MeshVariantViewModel>(Assert.Single(node.Variants));
        Assert.Equal(Client.Core.Models.VariantStatus.AwaitingApproval, variant.Status);
        Assert.Equal("concept.png", variant.ConceptImagePath);
        Assert.Null(variant.OutputFilePath);
        generationService.Verify(s => s.GenerateModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveConceptAsyncContinuesOnToMeshGeneration()
    {
        generationService
            .Setup(s => s.GenerateModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("model.glb");
        Client.ViewModels.MeshNodeViewModel node = new("m1", "Hero Statue") { RequireConceptApproval = true };
        Client.ViewModels.MeshVariantViewModel variant = new("v1", "Variant 1", Client.Core.Models.VariantStatus.AwaitingApproval, DateTimeOffset.UtcNow)
        {
            ConceptImagePath = "concept.png",
        };
        node.Variants.Add(variant);

        await generator.ApproveConceptAsync("/tmp/project", node, variant);

        Assert.Equal(Client.Core.Models.VariantStatus.Completed, variant.Status);
        Assert.Equal("model.glb", variant.OutputFilePath);
    }

    [Fact]
    public async Task RegenerateMeshOnlyAsyncKeepsTheExistingConceptImage()
    {
        generationService
            .Setup(s => s.GenerateModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("model-2.glb");
        Client.ViewModels.MeshNodeViewModel node = new("m1", "Hero Statue");
        Client.ViewModels.MeshVariantViewModel variant = new("v1", "Variant 1", Client.Core.Models.VariantStatus.Completed, DateTimeOffset.UtcNow)
        {
            ConceptImagePath = "concept.png",
            OutputFilePath = "model-1.glb",
        };
        node.Variants.Add(variant);

        await generator.RegenerateMeshOnlyAsync("/tmp/project", node, variant);

        Assert.Equal("concept.png", variant.ConceptImagePath);
        Assert.Equal("model-2.glb", variant.OutputFilePath);
        generationService.Verify(s => s.GenerateImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
