namespace Markwardt.AssetPipeline.Client;

/// <summary>
/// Contains the application entry point and Avalonia bootstrap configuration.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// The application entry point.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Builds and configures the Avalonia <see cref="AppBuilder"/> used to start the application.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { OverlayPopups = true })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
