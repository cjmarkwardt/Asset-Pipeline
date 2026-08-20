namespace Markwardt.AssetPipeline.Client.Infrastructure;

/// <summary>
/// Maps a project node's runtime type to the icon geometry resource used for it in the sidebar tree, so the
/// tree item template can bind straight to a <see cref="Avalonia.Controls.Shapes.Path.Data"/> without any
/// per-type XAML branching.
/// </summary>
internal sealed class ProjectNodeIconConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = value switch
        {
            RootNodeViewModel => "HomeIconGeometry",
            GroupNodeViewModel => "FolderIconGeometry",
            TextNodeViewModel => "TextIconGeometry",
            ImageNodeViewModel => "ImageIconGeometry",
            MeshNodeViewModel => "MeshIconGeometry",
            _ => null,
        };

        return key is not null && Application.Current is { } app && app.TryFindResource(key, out object? resource) ? resource : null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
