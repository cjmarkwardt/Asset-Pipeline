namespace Markwardt.AssetPipeline.Client.ViewModels.Infrastructure;

/// <summary>
/// Seam for anything that needs a native window (folder picker, modal dialogs), so view models stay
/// Avalonia-free.
/// </summary>
internal interface IDialogService
{
    /// <summary>
    /// Prompts the user to pick a folder.
    /// </summary>
    /// <param name="startDirectory">
    /// The directory the picker opens into, if it is non-null and still exists. Otherwise the picker
    /// opens to its own platform default location.
    /// </param>
    /// <returns>The picked folder's full path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickFolderAsync(string? startDirectory = null);

    /// <summary>
    /// Prompts the user to pick a single file.
    /// </summary>
    /// <param name="startDirectory">
    /// The directory the picker opens into, if it is non-null and still exists. Otherwise the picker opens
    /// to its own platform default location.
    /// </param>
    /// <returns>
    /// The picked file's full local path, or <see langword="null"/> if the user cancelled or the picked item
    /// is not a local file.
    /// </returns>
    Task<string?> PickFileAsync(string? startDirectory = null);

    /// <summary>
    /// Shows a modal message dialog with a single acknowledgement button.
    /// </summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Shows a modal Yes/No confirmation dialog.
    /// </summary>
    /// <param name="title">The dialog window's title.</param>
    /// <param name="message">The question posed to the user.</param>
    /// <param name="confirmText">The label for the affirmative button.</param>
    /// <returns><see langword="true"/> if the user confirmed; otherwise <see langword="false"/>.</returns>
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Yes");

    /// <summary>
    /// Shows a modal dialog prompting for a single line of text.
    /// </summary>
    /// <param name="title">The dialog window's title.</param>
    /// <param name="message">The prompt shown above the text field.</param>
    /// <param name="initialValue">The text field's initial value.</param>
    /// <returns>The entered text, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> ShowTextInputAsync(string title, string message, string initialValue = "");

    /// <summary>
    /// Shows a modal dialog for picking one node from <paramref name="candidates"/>.
    /// </summary>
    /// <param name="title">The dialog window's title.</param>
    /// <param name="candidates">The nodes the user may pick from.</param>
    /// <returns>The picked node, or <see langword="null"/> if the user cancelled.</returns>
    Task<ProjectNodeViewModel?> PickNodeAsync(string title, IReadOnlyList<ProjectNodeViewModel> candidates);

    /// <summary>
    /// Shows the modal Options dialog for configuring access to the scenario.com and meshy.ai APIs.
    /// </summary>
    Task ShowOptionsAsync();
}
