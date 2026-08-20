namespace Markwardt.AssetPipeline.Client.Views.Dialogs;

/// <summary>
/// A modal dialog for picking one node from a candidate list, with OK/Cancel buttons.
/// </summary>
internal sealed partial class NodePickerWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodePickerWindow"/> class.
    /// </summary>
    public NodePickerWindow()
    {
        InitializeComponent();
        this.DisableMinimize();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is NodePickerViewModel vm)
        {
            vm.RequestClose += node => Close(node);
        }
    }
}
