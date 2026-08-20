namespace Markwardt.AssetPipeline.Client.Views.Dialogs;

/// <summary>
/// A modal message dialog with a single acknowledgement button.
/// </summary>
internal sealed partial class MessageDialogWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialogWindow"/> class.
    /// </summary>
    public MessageDialogWindow()
    {
        InitializeComponent();
        this.DisableMinimize();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MessageDialogViewModel vm)
        {
            vm.RequestClose += () => Close();
        }
    }
}
