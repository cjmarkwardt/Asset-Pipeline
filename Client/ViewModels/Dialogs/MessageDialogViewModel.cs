namespace Markwardt.AssetPipeline.Client.ViewModels.Dialogs;

/// <summary>
/// Backs a modal message dialog with a single acknowledgement button.
/// </summary>
internal sealed partial class MessageDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Raised when the dialog should close, after the user acknowledges the message.
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// Gets the dialog window's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the message shown to the user.
    /// </summary>
    public required string Message { get; init; }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
