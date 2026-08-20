namespace Markwardt.AssetPipeline.Client.ViewModels.Dialogs;

/// <summary>
/// Backs a modal dialog for picking one node from a candidate list, with OK/Cancel buttons.
/// </summary>
internal sealed partial class NodePickerViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodePickerViewModel"/> class.
    /// </summary>
    public NodePickerViewModel(IReadOnlyList<ProjectNodeViewModel> candidates)
    {
        Candidates = candidates;
        selected = candidates.FirstOrDefault();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ProjectNodeViewModel? selected;

    /// <summary>
    /// Raised when the dialog should close. The payload is the picked node if the user confirmed, or
    /// <see langword="null"/> if they cancelled.
    /// </summary>
    public event Action<ProjectNodeViewModel?>? RequestClose;

    /// <summary>
    /// Gets the dialog window's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the nodes the user may pick from.
    /// </summary>
    public IReadOnlyList<ProjectNodeViewModel> Candidates { get; }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm() => RequestClose?.Invoke(Selected);

    private bool CanConfirm() => Selected is not null;

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
