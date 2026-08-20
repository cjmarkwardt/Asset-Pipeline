namespace Markwardt.AssetPipeline.Client.ViewModels.Infrastructure;

/// <summary>
/// The combined context text and reference files gathered for one node by <see cref="NodeContextAggregator"/>.
/// </summary>
internal sealed record AggregatedContext(string Text, IReadOnlyList<ReferenceFileViewModel> ReferenceFiles);

/// <summary>
/// Gathers the full context an <see cref="AssetNodeViewModel"/> should generate a variant from: its own
/// context and reference files, everything from its parent chain up to the root, and everything from each
/// linked node's own parent chain - with every source appearing at most once, even if reachable both through
/// the node's own ancestry and through a link's ancestry.
/// </summary>
internal static class NodeContextAggregator
{
    /// <summary>
    /// Collects <paramref name="node"/>'s aggregated context.
    /// </summary>
    public static AggregatedContext Collect(ProjectNodeViewModel node)
    {
        List<ProjectNodeViewModel> sources = [];
        HashSet<ProjectNodeViewModel> seen = [];

        void AddAncestorChain(ProjectNodeViewModel start)
        {
            for (ProjectNodeViewModel? current = start; current is not null; current = current.Parent)
            {
                if (seen.Add(current))
                {
                    sources.Add(current);
                }
            }
        }

        AddAncestorChain(node);
        foreach (ProjectNodeViewModel link in node.Links)
        {
            AddAncestorChain(link);
        }

        StringBuilder text = new();
        List<ReferenceFileViewModel> referenceFiles = [];
        foreach (ProjectNodeViewModel source in sources)
        {
            referenceFiles.AddRange(source.ReferenceFiles);

            if (string.IsNullOrWhiteSpace(source.Context))
            {
                continue;
            }

            text.AppendLine($"[{source.Name}]");
            text.AppendLine(source.Context);

            if (source.ReferenceFiles.Count > 0)
            {
                text.AppendLine($"Reference files: {string.Join(", ", source.ReferenceFiles.Select(file => file.Name))}");
            }

            text.AppendLine();
        }

        return new AggregatedContext(text.ToString().TrimEnd(), referenceFiles);
    }
}
