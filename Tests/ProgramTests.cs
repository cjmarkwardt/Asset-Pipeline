namespace Markwardt.AssetPipeline.Tests;

/// <summary>
/// Tests for <see cref="Client.Program"/>.
/// </summary>
public sealed class ProgramTests
{
    /// <summary>
    /// Verifies that the Avalonia application builder is constructed with the <see cref="Client.App"/> entry point.
    /// </summary>
    [Fact]
    public void BuildAvaloniaAppReturnsConfiguredBuilder()
    {
        AppBuilder builder = Client.Program.BuildAvaloniaApp();

        Assert.NotNull(builder);
    }
}
