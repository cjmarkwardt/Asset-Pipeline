namespace Markwardt.AssetPipeline.Client.Views.Dialogs;

/// <summary>
/// A modal Yes/No confirmation dialog.
/// </summary>
internal sealed partial class ConfirmDialogWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmDialogWindow"/> class.
    /// </summary>
    public ConfirmDialogWindow()
    {
        InitializeComponent();
        this.DisableMinimize();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is ConfirmDialogViewModel vm)
        {
            vm.RequestClose += confirmed => Close(confirmed);
        }
    }
}
