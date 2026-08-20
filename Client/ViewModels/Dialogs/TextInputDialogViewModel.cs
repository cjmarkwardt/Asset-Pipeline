namespace Markwardt.AssetPipeline.Client.ViewModels.Dialogs;

/// <summary>
/// Backs a modal dialog prompting for a single line of text, with OK/Cancel buttons.
/// </summary>
internal sealed partial class TextInputDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string value = "";

    /// <summary>
    /// Raised when the dialog should close. The payload is the entered <see cref="Value"/> if the user
    /// confirmed, or <see langword="null"/> if they cancelled.
    /// </summary>
    public event Action<string?>? RequestClose;

    /// <summary>
    /// Gets the dialog window's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the prompt shown above the text field.
    /// </summary>
    public required string Message { get; init; }

    [RelayCommand]
    private void Confirm() => RequestClose?.Invoke(Value);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
