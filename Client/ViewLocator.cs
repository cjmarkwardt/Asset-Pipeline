namespace Markwardt.AssetPipeline.Client;

/// <summary>
/// Resolves a view model to its view by naming convention: a <c>ViewModels.FooViewModel</c> maps to
/// <c>Views.FooView</c>.
/// </summary>
internal sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "(null view model)" };
        }

        string name = param.GetType().FullName!
            .Replace("Markwardt.AssetPipeline.Client.ViewModels", "Markwardt.AssetPipeline.Client.Views")
            .Replace("ViewModel", "View");

        Type? type = Type.GetType(name);
        try
        {
            if (type is not null && Activator.CreateInstance(type) is Control control)
            {
                return control;
            }
        }
        catch (Exception ex)
        {
            return new TextBlock { Text = $"DEBUG EX: {ex}", Foreground = Avalonia.Media.Brushes.Red, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        }

        return new TextBlock { Text = $"View not found: {name}" };
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModelBase;
}
