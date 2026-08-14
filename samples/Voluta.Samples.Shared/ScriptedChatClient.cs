namespace Voluta.Samples.Shared;

/// <summary>
///     Deterministic offline replies (no API key).
/// </summary>
/// <remarks>
///     Matches on the system-prompt role first so long user bodies
///     (evidence / sources) do not flip the scripted branch.
///     More specific roles (marketing, docs) are checked before generic
///     planner/reviewer keywords.
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

        if (system.Contains("trading-desk campaign planner", StringComparison.OrdinalIgnoreCase)
            || system.Contains("Hybrid.ai trading-desk", StringComparison.OrdinalIgnoreCase)
            || system.Contains("marketing campaign planner", StringComparison.OrdinalIgnoreCase)
            || system.Contains("campaign planner", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                objective: awareness + site traffic for Hybrid console self-serve
                audience: media buyers / agencies in RU, desktop+mobile web
                campaignType: HybridExtended (display)
                ssp: Yandex + Between (allow-list), optional Yandex Premium deal
                bet: fixed CPM (NoOptimization), daily Budget cap
                success: eCPM stable, DeliveryStatus=Active, no MoneyEnded
                """);
        }

        if (system.Contains("AdLibrary creative writer", StringComparison.OrdinalIgnoreCase)
            || system.Contains("ad creative writer", StringComparison.OrdinalIgnoreCase)
            || system.Contains("creative writer", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                adLibrary: 300x250_brand_v1
                format: Display
                headline: Hybrid Console — один trading desk для SSP и прямых сделок
                body: Кампании, баннеры, лимиты и DeliveryStatus в одном .NET API.
                cta: Открыть console
                size: 300x250
                """);
        }

        if (system.Contains("trading-desk reviewer", StringComparison.OrdinalIgnoreCase)
            || system.Contains("marketing reviewer", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                go / no-go: GO
                rationale: draft→Active with Approved banner, Yandex+Between LinkedSystems, daily Budget set, DeliveryStatus healthy.
                """);
        }

        if (system.Contains("documentation", StringComparison.OrdinalIgnoreCase)
            || system.Contains("answer documentation", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                """
                answer: Voluta is a stateful agent graph runtime for .NET (Pregel supersteps, channels, InMemory checkpoints).
                sources: README.md
                need_tools: false
                """);
        }

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

        // Fallback when system role does not match known sample roles.
        return Task.FromResult("ok (scripted offline reply)");
    }
}
