namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for the type-specific settings on <see cref="Client.ViewModels.TextNodeViewModel"/> and
/// <see cref="Client.ViewModels.MeshNodeViewModel"/>, and their round trip through <c>ToModel</c>/<c>FromModel</c>.
/// </summary>
public sealed class AssetNodeViewModelTests
{
    [Fact]
    public void TextNodeToModelThenFromModelRoundTripsItsSettings()
    {
        Client.ViewModels.TextNodeViewModel node = new("t1", "Hero Bio", "Context", "Generated/Hero")
        {
            Unit = Client.Core.Models.TextUnit.Sentences,
            MinUnits = 3,
            MaxUnits = 10,
            IsJsonFormatted = true,
            JsonSchema = "{ \"type\": \"object\" }",
        };

        Client.Core.Models.ProjectNode model = node.ToModel();
        Client.ViewModels.ProjectNodeViewModel rebuilt = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");

        Client.ViewModels.TextNodeViewModel rebuiltText = Assert.IsType<Client.ViewModels.TextNodeViewModel>(rebuilt);
        Assert.Equal(Client.Core.Models.TextUnit.Sentences, rebuiltText.Unit);
        Assert.Equal(3, rebuiltText.MinUnits);
        Assert.Equal(10, rebuiltText.MaxUnits);
        Assert.True(rebuiltText.IsJsonFormatted);
        Assert.Equal("{ \"type\": \"object\" }", rebuiltText.JsonSchema);
        Assert.Equal("Generated/Hero", rebuiltText.OutputPath);
    }

    [Fact]
    public void MeshNodeToModelThenFromModelRoundTripsRequireConceptApproval()
    {
        Client.ViewModels.MeshNodeViewModel node = new("m1", "Hero Statue") { RequireConceptApproval = true };

        Client.Core.Models.ProjectNode model = node.ToModel();
        Client.ViewModels.ProjectNodeViewModel rebuilt = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");

        Client.ViewModels.MeshNodeViewModel rebuiltMesh = Assert.IsType<Client.ViewModels.MeshNodeViewModel>(rebuilt);
        Assert.True(rebuiltMesh.RequireConceptApproval);
    }

    [Fact]
    public void AssetNodeToModelThenFromModelRoundTripsVariants()
    {
        Client.ViewModels.TextNodeViewModel node = new("t1", "Hero Bio");
        node.Variants.Add(new Client.ViewModels.TextVariantViewModel("v1", "Variant 1", Client.Core.Models.VariantStatus.Completed, DateTimeOffset.UtcNow)
        {
            OutputFilePath = "v1.txt",
        });

        Client.Core.Models.ProjectNode model = node.ToModel();
        Client.ViewModels.ProjectNodeViewModel rebuilt = Client.ViewModels.ProjectNodeViewModel.FromModel(model, "/tmp/project");

        Client.ViewModels.TextNodeViewModel rebuiltText = Assert.IsType<Client.ViewModels.TextNodeViewModel>(rebuilt);
        Client.ViewModels.TextVariantViewModel rebuiltVariant = Assert.IsType<Client.ViewModels.TextVariantViewModel>(Assert.Single(rebuiltText.Variants));
        Assert.Equal("Variant 1", rebuiltVariant.Name);
        Assert.Equal(Client.Core.Models.VariantStatus.Completed, rebuiltVariant.Status);
        Assert.Equal("v1.txt", rebuiltVariant.OutputFilePath);
    }
}
