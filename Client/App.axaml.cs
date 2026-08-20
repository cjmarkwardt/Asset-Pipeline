namespace Markwardt.AssetPipeline.Client;

/// <summary>
/// The Avalonia application object responsible for global styles, dependency injection, and startup.
/// </summary>
public sealed partial class App : Application
{
    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();

        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IProjectDataStore, ProjectDataStore>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<ITextGenerationService, ClaudeCodeTextGenerationService>();
        services.AddSingleton<IImageGenerationService, ScenarioImageGenerationService>();
        services.AddSingleton<IScenarioModelCatalogService, ScenarioModelCatalogService>();
        services.AddSingleton<IScenarioModelPickerService, ClaudeScenarioModelPickerService>();
        services.AddSingleton<IModelGenerationService, MeshyModelGenerationService>();
        services.AddSingleton<IAssetGenerationService, AssetGenerationService>();
        services.AddSingleton<IAssetVariantGenerator, AssetVariantGenerator>();

        services.AddSingleton<IProjectTabFactory, ProjectTabFactory>();
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    private ServiceProvider? services;
    private MainWindowViewModel? mainWindowViewModel;
    private bool shutdownConfirmed;

    /// <inheritdoc />
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            services = BuildServiceProvider();
            mainWindowViewModel = services.GetRequiredService<MainWindowViewModel>();

            MainWindow window = new() { DataContext = mainWindowViewModel };
            desktop.MainWindow = window;

            desktop.ShutdownRequested += OnShutdownRequested;

            // Backstop for any exit path that never reaches OnShutdownRequested at all - a caught
            // termination signal or an unhandled-exception crash still raises ProcessExit.
            // ServiceProvider.Dispose is idempotent, so it is safe for this to fire in addition to the
            // normal path below.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeServices();

            _ = mainWindowViewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (shutdownConfirmed || mainWindowViewModel is null)
        {
            return;
        }

        e.Cancel = true;
        await mainWindowViewModel.ShutdownAsync();
        shutdownConfirmed = true;

        // Disposes every registered singleton that implements IDisposable - done here, on the one path
        // that's guaranteed to run for a normal window close, rather than only in the ProcessExit backstop
        // above.
        DisposeServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void DisposeServices() => services?.Dispose();
}
