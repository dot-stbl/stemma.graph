namespace StemmaGraph.Samples.Shared;

/// <summary>
///     Deterministic offline replies (no API key).
/// </summary>
/// <remarks>
///     Matches on the system-prompt role first so long user bodies
///     (evidence / sources) do not flip the scripted branch.
/// </remarks>
public sealed class ScriptedChatClient : IChatCompletionClient
{
    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var system = systemPrompt ?? string.Empty;

        if (system.Contains("planner", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                checklist:
                - correctness
                - security
                - naming
                tools: read,search
                """);
        }

        if (system.Contains("reviewer", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                findings:
                1. [medium] Prefer IReadOnlyList on public APIs.
                2. [low] Add a regression test for the new branch.
                verdict: comment_ready
                need_tools: false
                """);
        }

        if (system.Contains("documentation", StringComparison.OrdinalIgnoreCase)
            || system.Contains("answer documentation", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                answer: StemmaGraph is a stateful agent graph runtime for .NET (Pregel supersteps, channels, InMemory checkpoints).
                sources: README.md
                need_tools: false
                """);
        }

        // Fallback: short keyword on user (harness tests without role system lines).
        _ = userPrompt;
        return Task.FromResult("ok (scripted offline reply)");
    }
}
