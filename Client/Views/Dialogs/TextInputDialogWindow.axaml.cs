namespace Markwardt.AssetPipeline.Client.Views.Dialogs;

/// <summary>
/// A modal dialog prompting for a single line of text, with OK/Cancel buttons.
/// </summary>
internal sealed partial class TextInputDialogWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputDialogWindow"/> class.
    /// </summary>
    public TextInputDialogWindow()
    {
        InitializeComponent();
        this.DisableMinimize();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is TextInputDialogViewModel vm)
        {
            vm.RequestClose += value => Close(value);
        }

        this.FindControl<TextBox>("ValueBox")?.Focus();
    }
}
