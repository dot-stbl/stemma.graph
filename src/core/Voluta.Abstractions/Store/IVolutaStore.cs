namespace Voluta.Abstractions.Store;

/// <summary>
///     Process- or host-scoped key-value store shared across graph threads (LangGraph-class
///     BaseStore parity). Distinct from <see cref="Checkpoint.ICheckpointer" />, which is
///     per-thread C-shape run state.
/// </summary>
/// <remarks>
///     Nodes resolve the store from <c>GraphContext.Services</c> when the host registers
///     <see cref="IVolutaStore" /> (e.g. <c>AddVoluta</c> + <c>Store.UseInMemory()</c>).
///     Concurrent Put/Get from different threads MUST be safe for a given implementation.
/// </remarks>
public interface IVolutaStore
{
    /// <summary>
    ///     Upserts <paramref name="value" /> at <paramref name="namespace" /> + <paramref name="key" />.
    /// </summary>
    /// <param name="namespace">Hierarchical namespace (empty list = root). Segments are ordinal.</param>
    /// <param name="key">Item key within the namespace.</param>
    /// <param name="value">Value to store (may be null).</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public Task PutAsync(
        IReadOnlyList<string> @namespace,
        string key,
        object? value,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads an item, or null when the key was never put (miss MUST NOT throw).
    /// </summary>
    /// <param name="namespace">Hierarchical namespace (empty list = root).</param>
    /// <param name="key">Item key within the namespace.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The stored item, or null if not found.</returns>
    public Task<StoreItem?> GetAsync(
        IReadOnlyList<string> @namespace,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists all items under an exact namespace (not a prefix search). Ordered by key ascending.
    ///     An empty namespace with no items returns an empty list.
    /// </summary>
    /// <param name="namespace">Hierarchical namespace (empty list = root).</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Items in the namespace, key-ordered.</returns>
    public Task<IReadOnlyList<StoreItem>> ListAsync(
        IReadOnlyList<string> @namespace,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes the item at <paramref name="namespace" /> + <paramref name="key" />.
    ///     Missing keys are a no-op (MUST NOT throw).
    /// </summary>
    /// <param name="namespace">Hierarchical namespace (empty list = root).</param>
    /// <param name="key">Item key within the namespace.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public Task DeleteAsync(
        IReadOnlyList<string> @namespace,
        string key,
        CancellationToken cancellationToken = default);
}
