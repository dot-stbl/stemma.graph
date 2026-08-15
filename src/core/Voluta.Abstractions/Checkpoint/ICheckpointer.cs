namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     Pluggable durable store for C-shape checkpoints. Implementations live in core
///     (InMemory) or provider packages (EF, S3, File); this assembly has no storage deps.
/// </summary>
public interface ICheckpointer
{
    /// <summary>
    ///     Persists a checkpoint for the snapshot's thread (and step history when supported).
    /// </summary>
    /// <param name="snapshot">Full C-shape snapshot to store.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>A task that completes when the snapshot is durable for the provider.</returns>
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads the latest checkpoint for a thread, or null when the thread was never put.
    ///     A miss MUST NOT throw; storage outages may still throw.
    /// </summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The latest snapshot, or null if not found.</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Failure checkpoint policy:</strong> after a node/superstep failure the
    ///         runtime puts a terminal <see cref="Runtime.GraphRunStatus.Failed" /> (or
    ///         <see cref="Runtime.GraphRunStatus.Cancelled" />) snapshot at the failing
    ///         superstep index. That document's channel values are the last successfully
    ///         applied superstep — incomplete writes are never merged. Get therefore returns
    ///         status Failed/Cancelled with last-good payload, not a wiped or half-applied
    ///         state. Providers MUST NOT overwrite a prior step's document when putting a
    ///         higher step (keyed by thread + step).
    ///     </para>
    ///     <para>
    ///         Use <see cref="ListAsync" /> when supported to inspect earlier successful
    ///         Running/Interrupted/Done steps for recovery tooling.
    ///     </para>
    /// </remarks>
    public Task<CheckpointSnapshot?> GetAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Optionally lists checkpoints for a thread (time-travel), ordered by step ascending.
    ///     Providers that do not support history MUST throw <see cref="NotSupportedException" />
    ///     rather than return partial silent data.
    /// </summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Checkpoints for the thread, oldest step first when supported.</returns>
    public Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default);
}
