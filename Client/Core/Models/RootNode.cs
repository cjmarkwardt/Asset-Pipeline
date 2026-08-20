namespace Markwardt.AssetPipeline.Client.Core.Models;

/// <summary>
/// The single, undeletable node every project tree starts with. Behaves exactly like a
/// <see cref="GroupNode"/> everywhere in the UI, aside from being the one node that can never be deleted.
/// </summary>
internal sealed record RootNode : GroupNode;
