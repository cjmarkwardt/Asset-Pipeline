namespace Markwardt.AssetPipeline.Client.Views;

/// <summary>
/// The application shell: the custom title bar (project launcher, open project tabs, window controls) and
/// the selected project's own content area.
/// </summary>
internal sealed partial class MainShellView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainShellView"/> class.
    /// </summary>
    public MainShellView() => InitializeComponent();

    /// <summary>
    /// The title bar's own empty background drags/double-click-maximizes the window like a real OS
    /// titlebar would, since <see cref="MainWindow"/> draws no titlebar of its own. Interactive children
    /// (buttons, tab items) do not mark PointerPressed Handled during the press phase (only their eventual
    /// Click does, on release) - dragging unconditionally on every bubbled press would hijack pointer
    /// capture out from under them before that Click ever fires. So this explicitly skips
    /// dragging/maximizing whenever the press originated from an interactive descendant.
    /// </summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is Visual sourceVisual &&
            (sourceVisual.FindAncestorOfType<Button>(includeSelf: true) is not null ||
             sourceVisual.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null))
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        window.BeginMoveDrag(e);
    }

    private void OnRecentBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainShellViewModel vm)
        {
            vm.Header.CloseRecentMenu();
        }
    }

    /// <summary>Stops a click inside the dropdown's own body from bubbling to the backdrop and closing it.</summary>
    private void OnRecentPopupPointerPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;

    private void OnRecentRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // The remove (x) button lives inside this same row - let its own Click/Command handle that case
        // instead of also opening the project it just removed.
        if (e.Source is Visual sourceVisual && sourceVisual.FindAncestorOfType<Button>(includeSelf: true) is not null)
        {
            return;
        }

        if (sender is StyledElement { DataContext: ProjectInfo project } && DataContext is MainShellViewModel vm)
        {
            vm.Header.OpenRecentCommand.Execute(project);
        }
    }
}
