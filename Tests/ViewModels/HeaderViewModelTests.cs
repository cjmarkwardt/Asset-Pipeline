namespace Markwardt.AssetPipeline.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="Client.ViewModels.HeaderViewModel"/>.
/// </summary>
public sealed class HeaderViewModelTests
{
    private readonly Mock<Client.Core.Services.IProjectService> projectService = new();
    private readonly Mock<Client.ViewModels.Infrastructure.IDialogService> dialogService = new();

    private Client.ViewModels.HeaderViewModel CreateHeader() => new(projectService.Object, dialogService.Object);

    [Fact]
    public async Task OpenPathAsyncForAnInvalidFolderAsksToCreateAndDoesNotRaiseProjectOpenedWhenDeclined()
    {
        projectService.Setup(s => s.OpenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client.Core.Models.ProjectInfo?)null);
        dialogService.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        Client.ViewModels.HeaderViewModel header = CreateHeader();
        bool raised = false;
        header.ProjectOpened += _ => raised = true;

        await header.OpenPathAsync("/tmp/not-a-project");

        Assert.False(raised);
        dialogService.Verify(d => d.ShowConfirmAsync(It.IsAny<string>(), It.Is<string>(m => m.Contains("/tmp/not-a-project")), It.IsAny<string>()), Times.Once);
        projectService.Verify(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenPathAsyncForAnInvalidFolderCreatesAndOpensTheProjectWhenConfirmed()
    {
        Client.Core.Models.ProjectInfo created = new() { FullPath = "/tmp/not-a-project" };
        projectService.Setup(s => s.OpenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client.Core.Models.ProjectInfo?)null);
        dialogService.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        projectService.Setup(s => s.CreateAsync(created.FullPath, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        projectService.Setup(s => s.GetRecentProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([created]);
        Client.ViewModels.HeaderViewModel header = CreateHeader();
        Client.Core.Models.ProjectInfo? opened = null;
        header.ProjectOpened += p => opened = p;

        await header.OpenPathAsync(created.FullPath);

        Assert.Same(created, opened);
    }

    [Fact]
    public async Task OpenPathAsyncForAValidFolderRaisesProjectOpenedAndRefreshesRecents()
    {
        Client.Core.Models.ProjectInfo project = new() { FullPath = "/tmp/a-project" };
        projectService.Setup(s => s.OpenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(project);
        projectService.Setup(s => s.GetRecentProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);
        Client.ViewModels.HeaderViewModel header = CreateHeader();
        Client.Core.Models.ProjectInfo? opened = null;
        header.ProjectOpened += p => opened = p;

        await header.OpenPathAsync(project.FullPath);

        Assert.Same(project, opened);
        Assert.Same(project, Assert.Single(header.RecentProjects));
        dialogService.Verify(d => d.ShowMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToggleRecentMenuCommandTogglesIsRecentMenuOpen()
    {
        Client.ViewModels.HeaderViewModel header = CreateHeader();

        header.ToggleRecentMenuCommand.Execute(null);
        Assert.True(header.IsRecentMenuOpen);

        header.ToggleRecentMenuCommand.Execute(null);
        Assert.False(header.IsRecentMenuOpen);
    }

    [Fact]
    public async Task RemoveRecentAsyncForgetsTheProjectAndRefreshesRecents()
    {
        Client.Core.Models.ProjectInfo project = new() { FullPath = "/tmp/a-project" };
        projectService.SetupSequence(s => s.GetRecentProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([project])
            .ReturnsAsync((IReadOnlyList<Client.Core.Models.ProjectInfo>)[]);
        Client.ViewModels.HeaderViewModel header = CreateHeader();
        await header.RefreshRecentProjectsAsync();

        await header.RemoveRecentCommand.ExecuteAsync(project);

        projectService.Verify(s => s.ForgetRecentAsync(project.FullPath, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(header.RecentProjects);
    }
}
