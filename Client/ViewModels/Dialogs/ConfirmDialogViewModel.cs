namespace Markwardt.AssetPipeline.Client.ViewModels.Dialogs;

/// <summary>
/// Backs a modal Yes/No confirmation dialog.
/// </summary>
internal sealed partial class ConfirmDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Raised when the dialog should close. The payload is <see langword="true"/> if the user confirmed,
    /// or <see langword="false"/> if they cancelled.
    /// </summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// Gets the dialog window's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the question posed to the user.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the label for the affirmative button.
    /// </summary>
    public string ConfirmText { get; init; } = "Yes";

    /// <summary>
    /// Gets the label for the negative button.
    /// </summary>
    public string CancelText { get; init; } = "Cancel";

    [RelayCommand]
    private void Confirm() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
