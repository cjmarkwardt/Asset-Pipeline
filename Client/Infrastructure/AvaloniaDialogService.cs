namespace Markwardt.AssetPipeline.Client.Infrastructure;

/// <inheritdoc cref="IDialogService" />
internal sealed class AvaloniaDialogService(ISettingsService settingsService) : IDialogService
{
    private static Window OwnerWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new InvalidOperationException("Main window is not available yet.");

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string? startDirectory = null)
    {
        IStorageFolder? suggestedStartLocation = startDirectory is null
            ? null
            : await OwnerWindow.StorageProvider.TryGetFolderFromPathAsync(startDirectory);

        IReadOnlyList<IStorageFolder> folders = await OwnerWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Project Folder",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> PickFileAsync(string? startDirectory = null)
    {
        IStorageFolder? suggestedStartLocation = startDirectory is null
            ? null
            : await OwnerWindow.StorageProvider.TryGetFolderFromPathAsync(startDirectory);

        IReadOnlyList<IStorageFile> files = await OwnerWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Reference File",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message)
    {
        MessageDialogViewModel vm = new() { Title = title, Message = message };
        MessageDialogWindow window = new() { DataContext = vm };
        await window.ShowDialog(OwnerWindow);
    }

    /// <inheritdoc />
    public async Task<string?> ShowTextInputAsync(string title, string message, string initialValue = "")
    {
        TextInputDialogViewModel vm = new() { Title = title, Message = message, Value = initialValue };
        TextInputDialogWindow window = new() { DataContext = vm };
        return await window.ShowDialog<string?>(OwnerWindow);
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Yes")
    {
        ConfirmDialogViewModel vm = new() { Title = title, Message = message, ConfirmText = confirmText };
        ConfirmDialogWindow window = new() { DataContext = vm };
        return await window.ShowDialog<bool>(OwnerWindow);
    }

    /// <inheritdoc />
    public async Task<ProjectNodeViewModel?> PickNodeAsync(string title, IReadOnlyList<ProjectNodeViewModel> candidates)
    {
        NodePickerViewModel vm = new(candidates) { Title = title };
        NodePickerWindow window = new() { DataContext = vm };
        return await window.ShowDialog<ProjectNodeViewModel?>(OwnerWindow);
    }

    /// <inheritdoc />
    public async Task ShowOptionsAsync()
    {
        OptionsViewModel vm = new(settingsService);
        await vm.InitializeAsync();
        OptionsWindow window = new() { DataContext = vm };
        await window.ShowDialog(OwnerWindow);
    }
}
