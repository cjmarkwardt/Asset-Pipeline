namespace Markwardt.AssetPipeline.Client.ViewModels;

/// <summary>
/// A single reference file attached to a <see cref="GroupNodeViewModel"/>, editable in-place so the content
/// area's reference file list stays in sync.
/// </summary>
internal sealed partial class ReferenceFileViewModel : ViewModelBase
{
    /// <summary>
    /// Rebuilds an observable reference file from its persisted form.
    /// </summary>
    /// <param name="model">The persisted reference file to rebuild.</param>
    /// <param name="projectPath">The full path to the owning project's root folder, used to resolve this
    /// file's <see cref="AbsolutePath"/>.</param>
    public static ReferenceFileViewModel FromModel(ReferenceFile model, string projectPath) => new(
        model.Name,
        model.Source,
        model.Path,
        model.Source == ReferenceFileSource.Stored
            ? Path.Combine(projectPath, ProjectInfo.DataFolderName, model.Path)
            : Path.Combine(projectPath, model.Path));

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceFileViewModel"/> class.
    /// </summary>
    /// <param name="name">The user-assigned name this file is referenced by from context text.</param>
    /// <param name="source">Where this file is stored.</param>
    /// <param name="relativePath">The file's path, relative to <see cref="ReferenceFileSource.Stored"/>'s
    /// project data folder or <see cref="ReferenceFileSource.ProjectPath"/>'s project root folder.</param>
    /// <param name="absolutePath">The file's full, resolved path on disk.</param>
    public ReferenceFileViewModel(string name, ReferenceFileSource source, string relativePath, string absolutePath)
    {
        this.name = name;
        Source = source;
        RelativePath = relativePath;
        AbsolutePath = absolutePath;
    }

    private readonly HashSet<string> imageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    [ObservableProperty]
    private string name;

    /// <summary>
    /// Raised whenever this file's editable state changes in a way that needs to be persisted.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets where this file is stored.
    /// </summary>
    public ReferenceFileSource Source { get; }

    /// <summary>
    /// Gets the file's path, relative to the owning group node's project.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the file's full, resolved path on disk.
    /// </summary>
    public string AbsolutePath { get; }

    /// <summary>
    /// Gets a value indicating whether this file's extension identifies it as an image, meaning it should be
    /// displayed directly in the group node's content area rather than just listed by name.
    /// </summary>
    public bool IsImage => imageExtensions.Contains(System.IO.Path.GetExtension(RelativePath));

    /// <summary>
    /// Snapshots this reference file into its persisted form.
    /// </summary>
    public ReferenceFile ToModel() => new() { Name = Name, Source = Source, Path = RelativePath };

    partial void OnNameChanged(string value) => Changed?.Invoke();
}
