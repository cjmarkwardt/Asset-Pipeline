namespace Markwardt.AssetPipeline.Client.Core.Services;

/// <summary>
/// Shells out to the local <c>claude</c> CLI (Claude Code) for a single, non-interactive prompt/response
/// exchange, shared by every service that asks Claude to generate or decide something rather than calling a
/// hosted API. Relies on that CLI already being installed, on <c>PATH</c>, and authenticated in the
/// environment the client runs in.
/// </summary>
internal static class ClaudeCodeCli
{
    /// <summary>
    /// Runs <paramref name="prompt"/> through the CLI in one-shot, non-interactive mode.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The CLI's trimmed text response.</returns>
    /// <exception cref="InvalidOperationException">
    /// The CLI could not be started, or exited with a non-zero code.
    /// </exception>
    public static async Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new("claude")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(prompt);
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the local Claude Code CLI ('claude'). Ensure it is installed and on PATH.");

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        string output = await outputTask;
        string error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Claude Code CLI exited with code {process.ExitCode}: {error}");
        }

        return output.Trim();
    }
}
