namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// The application's top-level view model, wrapping the main shell (title bar and open project tabs).
/// </summary>
internal sealed class MainWindowViewModel(MainShellViewModel shell) : ViewModelBase
{
    /// <summary>
    /// Gets the main shell.
    /// </summary>
    public MainShellViewModel Shell { get; } = shell;

    /// <summary>
    /// Restores previously open projects on startup.
    /// </summary>
    public Task InitializeAsync() => Shell.InitializeAsync();

    /// <summary>
    /// Persists open projects and flushes pending saves before the app exits.
    /// </summary>
    public Task ShutdownAsync() => Shell.ShutdownAsync();
}
