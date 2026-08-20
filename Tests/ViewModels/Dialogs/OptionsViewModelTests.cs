namespace Markwardt.AssetPipeline.Tests.ViewModels.Dialogs;

/// <summary>
/// Tests for <see cref="Client.ViewModels.Dialogs.OptionsViewModel"/>.
/// </summary>
public sealed class OptionsViewModelTests
{
    private readonly Mock<Client.Core.Services.ISettingsService> settingsService = new();

    private Client.ViewModels.Dialogs.OptionsViewModel CreateOptions() => new(settingsService.Object);

    [Fact]
    public async Task InitializeAsyncLoadsThePersistedCredentials()
    {
        Client.Core.Models.AppSettings settings = new()
        {
            Api = new Client.Core.Models.ApiSettings { ScenarioApiKey = "key", ScenarioApiSecret = "secret", MeshyApiKey = "meshy-key" },
        };
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        Client.ViewModels.Dialogs.OptionsViewModel options = CreateOptions();

        await options.InitializeAsync();

        Assert.Equal("key", options.ScenarioApiKey);
        Assert.Equal("secret", options.ScenarioApiSecret);
        Assert.Equal("meshy-key", options.MeshyApiKey);
    }

    [Fact]
    public async Task SaveAsyncPersistsTheEditedCredentials()
    {
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Client.Core.Models.AppSettings());
        Client.ViewModels.Dialogs.OptionsViewModel options = CreateOptions();
        await options.InitializeAsync();
        options.ScenarioApiKey = "key";
        options.ScenarioApiSecret = "secret";
        options.MeshyApiKey = "meshy-key";
        Client.Core.Models.AppSettings? saved = null;
        settingsService.Setup(s => s.SaveAsync(It.IsAny<Client.Core.Models.AppSettings>(), It.IsAny<CancellationToken>()))
            .Callback<Client.Core.Models.AppSettings, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        await options.SaveCommand.ExecuteAsync(null);

        Assert.Equal("key", saved?.Api.ScenarioApiKey);
        Assert.Equal("secret", saved?.Api.ScenarioApiSecret);
        Assert.Equal("meshy-key", saved?.Api.MeshyApiKey);
    }

    [Fact]
    public void CancelRaisesRequestCloseWithoutSaving()
    {
        Client.ViewModels.Dialogs.OptionsViewModel options = CreateOptions();
        bool closed = false;
        options.RequestClose += () => closed = true;

        options.CancelCommand.Execute(null);

        Assert.True(closed);
        settingsService.Verify(s => s.SaveAsync(It.IsAny<Client.Core.Models.AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
