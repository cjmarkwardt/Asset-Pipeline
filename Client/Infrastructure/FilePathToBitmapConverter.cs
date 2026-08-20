namespace Markwardt.AssetPipeline.Client.Infrastructure;

/// <summary>
/// Converts a local file path into a <see cref="Bitmap"/> for display in an <see cref="Image"/> control,
/// used to preview an image reference file directly in a group node's content area.
/// </summary>
internal sealed class FilePathToBitmapConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
