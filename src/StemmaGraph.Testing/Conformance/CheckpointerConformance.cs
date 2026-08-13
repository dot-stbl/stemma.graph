using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Checkpoint;

namespace StemmaGraph.Testing.Conformance;

/// <summary>
///     Behavioral contract for <see cref="ICheckpointer" />. Call from provider unit tests
///     (InMemory now; EF/S3 later) without rewriting scenarios.
/// </summary>
public static class CheckpointerConformance
{
    /// <summary>
    ///     Put then Get MUST roundtrip C-shape fields for the same thread.
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task PutGetRoundtripAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        var threadId = $"conformance-roundtrip-{Guid.NewGuid():N}";
        var original = CreateSampleSnapshot(threadId, step: 3, status: GraphRunStatus.Running);

        await checkpointer.PutAsync(original, cancellationToken);
        var loaded = await checkpointer.GetAsync(threadId, cancellationToken) ?? throw new InvalidOperationException(
            "Conformance Put/Get: Get returned null after Put for the same thread.");
        AssertEqual(original, loaded);
    }

    /// <summary>
    ///     Get for an unknown thread MUST return null (not throw).
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task GetMissingReturnsNullAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        var threadId = $"conformance-missing-{Guid.NewGuid():N}";
        var loaded = await checkpointer.GetAsync(threadId, cancellationToken);
        if (loaded is not null)
        {
            throw new InvalidOperationException(
                "Conformance Get missing: expected null for unknown thread, got a snapshot.");
        }
    }

    /// <summary>
    ///     When List is supported, multiple Puts MUST appear ordered by step ascending.
    ///     Providers that throw <see cref="NotSupportedException" /> skip this scenario.
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task ListOrderedByStepWhenSupportedAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        var threadId = $"conformance-list-{Guid.NewGuid():N}";
        await checkpointer.PutAsync(
            CreateSampleSnapshot(threadId, step: 1, status: GraphRunStatus.Running),
            cancellationToken);
        await checkpointer.PutAsync(
            CreateSampleSnapshot(threadId, step: 2, status: GraphRunStatus.Done),
            cancellationToken);

        IReadOnlyList<CheckpointSnapshot> list;
        try
        {
            list = await checkpointer.ListAsync(threadId, cancellationToken);
        }
        catch (NotSupportedException)
        {
            return;
        }

        if (list.Count < 2)
        {
            throw new InvalidOperationException(
                $"Conformance List: expected ≥ 2 snapshots, got {list.Count}.");
        }

        for (var index = 1; index < list.Count; index++)
        {
            if (list[index].Step < list[index - 1].Step)
            {
                throw new InvalidOperationException(
                    "Conformance List: snapshots must be ordered by step ascending.");
            }
        }
    }

    /// <summary>
    ///     Interrupted status and interrupt payload MUST roundtrip.
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task InterruptFieldsRoundtripAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        var threadId = $"conformance-interrupt-{Guid.NewGuid():N}";
        var payload = new Dictionary<string, object?> { ["amount"] = 50 };
        var original = new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = 1,
            Status = GraphRunStatus.Interrupted,
            LastNode = "gate",
            NextNodes = ["gate"],
            InterruptPayload = payload,
            ChannelValues = new Dictionary<string, object?>(StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(StringComparer.Ordinal),
            VersionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>(StringComparer.Ordinal),
            PendingWrites = []
        };

        await checkpointer.PutAsync(original, cancellationToken);
        var loaded = await checkpointer.GetAsync(threadId, cancellationToken) ??
                     throw new InvalidOperationException("Conformance interrupt: Get returned null after Put.");
        if (loaded.Status != GraphRunStatus.Interrupted)
        {
            throw new InvalidOperationException(
                $"Conformance interrupt: expected Interrupted, got {loaded.Status}.");
        }

        if (loaded.InterruptPayload is null)
        {
            throw new InvalidOperationException("Conformance interrupt: InterruptPayload was null.");
        }
    }

    /// <summary>
    ///     Pending writes MUST roundtrip when present.
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task PendingWritesRoundtripAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        var threadId = $"conformance-pending-{Guid.NewGuid():N}";
        var original = CreateSampleSnapshot(threadId, step: 4, status: GraphRunStatus.Running);
        await checkpointer.PutAsync(original, cancellationToken);
        var loaded = await checkpointer.GetAsync(threadId, cancellationToken) ??
                     throw new InvalidOperationException("Conformance pending writes: Get returned null after Put.");
        if (loaded.PendingWrites.Count != original.PendingWrites.Count)
        {
            throw new InvalidOperationException(
                $"Conformance pending writes: expected {original.PendingWrites.Count}, got {loaded.PendingWrites.Count}.");
        }

        if (loaded.PendingWrites[0].TaskId != original.PendingWrites[0].TaskId
            || loaded.PendingWrites[0].ChannelName != original.PendingWrites[0].ChannelName)
        {
            throw new InvalidOperationException(
                "Conformance pending writes: TaskId/ChannelName mismatch after roundtrip.");
        }
    }

    /// <summary>
    ///     Runs all mandatory conformance scenarios against <paramref name="checkpointer" />.
    /// </summary>
    /// <param name="checkpointer">Provider under test.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task RunAllAsync(
        ICheckpointer checkpointer,
        CancellationToken cancellationToken = default)
    {
        await GetMissingReturnsNullAsync(checkpointer, cancellationToken);
        await PutGetRoundtripAsync(checkpointer, cancellationToken);
        await ListOrderedByStepWhenSupportedAsync(checkpointer, cancellationToken);
        await InterruptFieldsRoundtripAsync(checkpointer, cancellationToken);
        await PendingWritesRoundtripAsync(checkpointer, cancellationToken);
    }

    private static CheckpointSnapshot CreateSampleSnapshot(
        string threadId,
        long step,
        GraphRunStatus status)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = step,
            Status = status,
            ChannelValues = new Dictionary<string, object?>
            {
                ["messages"] = new List<object?> { "a" }
            },
            ChannelVersions = new Dictionary<string, long> { ["messages"] = 2 },
            VersionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>
            {
                ["agent"] = new Dictionary<string, long> { ["messages"] = 1 }
            },
            PendingWrites =
            [
                new PendingWrite { TaskId = "agent", ChannelName = "messages", Value = "x" }
            ],
            LastNode = "agent",
            NextNodes = ["tools"],
            InterruptPayload = null
        };
    }

    private static void AssertEqual(CheckpointSnapshot expected, CheckpointSnapshot actual)
    {
        if (!string.Equals(actual.ThreadId, expected.ThreadId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Conformance: ThreadId mismatch (expected '{expected.ThreadId}', got '{actual.ThreadId}').");
        }

        if (actual.Step != expected.Step)
        {
            throw new InvalidOperationException(
                $"Conformance: Step mismatch (expected {expected.Step}, got {actual.Step}).");
        }

        if (actual.Status != expected.Status)
        {
            throw new InvalidOperationException(
                $"Conformance: Status mismatch (expected {expected.Status}, got {actual.Status}).");
        }

        if (actual.LastNode != expected.LastNode)
        {
            throw new InvalidOperationException(
                $"Conformance: LastNode mismatch (expected '{expected.LastNode}', got '{actual.LastNode}').");
        }

        if (actual.NextNodes.Count != expected.NextNodes.Count
            || !actual.NextNodes.SequenceEqual(expected.NextNodes, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Conformance: NextNodes mismatch after roundtrip.");
        }

        if (!actual.ChannelVersions.TryGetValue("messages", out var version) || version != 2)
        {
            throw new InvalidOperationException("Conformance: ChannelVersions['messages'] expected 2.");
        }

        if (!actual.VersionsSeen.TryGetValue("agent", out var seen)
            || !seen.TryGetValue("messages", out var seenVersion)
            || seenVersion != 1)
        {
            throw new InvalidOperationException("Conformance: VersionsSeen['agent']['messages'] expected 1.");
        }
    }
}
