namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     Optional capability for enumerating known thread ids from durable (or in-process) storage.
/// </summary>
/// <remarks>
///     Additive to <see cref="ICheckpointer" /> so providers that cannot scan (or hosts that
///     only register <see cref="ICheckpointer" />) stay source-compatible. Ops UI and multi-host
///     tooling cast: <c>checkpointer is IThreadDiscovery discovery</c>.
/// </remarks>
public interface IThreadDiscovery
{
    /// <summary>
    ///     Lists thread identifiers that have at least one stored checkpoint, ordered ascending.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Distinct thread ids; empty when the store has none.</returns>
    /// <remarks>
    ///     A miss (empty store) MUST return an empty list, not throw. Storage outages may still throw
    ///     <see cref="CheckpointStoreException" />.
    /// </remarks>
    public Task<IReadOnlyList<string>> ListThreadIdsAsync(CancellationToken cancellationToken = default);
}
