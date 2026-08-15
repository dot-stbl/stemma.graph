using System.Collections.Concurrent;
using Voluta.Abstractions.Store;

namespace Voluta.Store;

/// <summary>
///     Process-local cross-thread <see cref="IVolutaStore" /> for tests and single-process samples.
/// </summary>
/// <remarks>
///     Values are held by reference (no deep clone). Concurrent Put/Get/List/Delete are safe.
///     Unlike durable checkpointers, InMemory does not enforce a wire-format value allow-list.
/// </remarks>
public sealed class InMemoryVolutaStore(TimeProvider? timeProvider = null) : IVolutaStore
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Entry>> namespaces =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task PutAsync(
        IReadOnlyList<string> @namespace,
        string key,
        object? value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = NamespacePath.Format(@namespace);
        var bucket = namespaces.GetOrAdd(path, static _ => new ConcurrentDictionary<string, Entry>(StringComparer.Ordinal));
        bucket[key] = new Entry(value, clock.GetUtcNow());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<StoreItem?> GetAsync(
        IReadOnlyList<string> @namespace,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = NamespacePath.Format(@namespace);
        return !namespaces.TryGetValue(path, out var bucket) || !bucket.TryGetValue(key, out var entry)
            ? Task.FromResult<StoreItem?>(null)
            : Task.FromResult<StoreItem?>(StoreItemFactory.Create(@namespace, key, entry));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StoreItem>> ListAsync(
        IReadOnlyList<string> @namespace,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = NamespacePath.Format(@namespace);
        if (!namespaces.TryGetValue(path, out var bucket) || bucket.IsEmpty)
        {
            return Task.FromResult<IReadOnlyList<StoreItem>>([]);
        }

        var items = bucket
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => StoreItemFactory.Create(@namespace, pair.Key, pair.Value))
            .ToArray();
        return Task.FromResult<IReadOnlyList<StoreItem>>(items);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        IReadOnlyList<string> @namespace,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = NamespacePath.Format(@namespace);
        if (namespaces.TryGetValue(path, out var bucket))
        {
            bucket.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    internal readonly record struct Entry(object? Value, DateTimeOffset UpdatedAt);
}

/// <summary>
///     Stable ordinal path encoding for hierarchical namespaces (unit separator).
/// </summary>
file static class NamespacePath
{
    private const char Separator = '\u001f';

    public static string Format(IReadOnlyList<string> @namespace)
    {
        return @namespace.Count == 0 ? string.Empty : string.Join(Separator, @namespace);
    }
}

/// <summary>
///     Maps internal entries to public <see cref="StoreItem" /> snapshots.
/// </summary>
file static class StoreItemFactory
{
    public static StoreItem Create(
        IReadOnlyList<string> @namespace,
        string key,
        InMemoryVolutaStore.Entry entry)
    {
        return new StoreItem
        {
            Namespace = [.. @namespace],
            Key = key,
            Value = entry.Value,
            UpdatedAt = entry.UpdatedAt,
        };
    }
}
