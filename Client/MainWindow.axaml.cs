namespace Markwardt.AssetPipeline.Client;

/// <summary>
/// The main application window. Draws no native chrome of its own - <see cref="Views.MainShellView"/> draws
/// the entire custom title bar, including drag-to-move/double-click-to-maximize handling.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() =>
        InitializeComponent();

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        WindowState = WindowState.Maximized;
    }
}
