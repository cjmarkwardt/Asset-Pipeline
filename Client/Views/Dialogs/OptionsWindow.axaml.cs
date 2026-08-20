namespace Markwardt.AssetPipeline.Client.Views.Dialogs;

/// <summary>
/// The modal Options dialog for configuring access to the scenario.com and meshy.ai APIs.
/// </summary>
internal sealed partial class OptionsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsWindow"/> class.
    /// </summary>
    public OptionsWindow()
    {
        InitializeComponent();
        this.DisableMinimize();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is OptionsViewModel vm)
        {
            vm.RequestClose += () => Close();
        }
    }
}
