namespace Markwardt.AssetPipeline.Client.Infrastructure;

/// <summary>
/// Extension helpers shared by this app's small modal utility dialogs.
/// </summary>
internal static class DialogWindowExtensions
{
    /// <summary>
    /// These small modal utility dialogs should never be minimized: an owned dialog commonly gets no
    /// taskbar entry of its own, so once minimized there is nothing left to click to bring it (and the app
    /// it's blocking) back to the foreground.
    /// </summary>
    public static void DisableMinimize(this Window window) => window.CanMinimize = false;
}
