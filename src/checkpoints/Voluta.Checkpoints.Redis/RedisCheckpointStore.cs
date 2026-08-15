using StackExchange.Redis;

namespace Voluta.Checkpoints.Redis;

/// <summary>
///     <see cref="IRedisCheckpointStore" /> over a live Redis connection.
/// </summary>
internal sealed class RedisCheckpointStore(IConnectionMultiplexer multiplexer, int database) : IRedisCheckpointStore
{
    /// <inheritdoc />
    public async Task PutAsync(string threadKey, long step, string json, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = multiplexer.GetDatabase(database);

        // Same-step re-puts replace the previous member: drop leftovers first so the
        // sorted set never holds two documents for one step.
        await db.SortedSetRemoveRangeByScoreAsync(threadKey, step, step);
        await db.SortedSetAddAsync(threadKey, json, step);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RedisCheckpointEntry>> ListAsync(
        string threadKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = multiplexer.GetDatabase(database);
        var entries = await db.SortedSetRangeByRankWithScoresAsync(threadKey, order: Order.Ascending);
        return entries
            .Select(static entry => new RedisCheckpointEntry((long)entry.Score, entry.Element.ToString()))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<RedisCheckpointEntry?> LatestAsync(
        string threadKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = multiplexer.GetDatabase(database);
        var entries = await db.SortedSetRangeByRankWithScoresAsync(
            threadKey,
            start: 0,
            stop: 0,
            order: Order.Descending);
        return entries.Length == 0
            ? null
            : new RedisCheckpointEntry((long)entries[0].Score, entries[0].Element.ToString());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListKeysAsync(string pattern, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var keys = new List<string>();
        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            var server = multiplexer.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(database, pattern: pattern, pageSize: 500)
                .WithCancellation(cancellationToken))
            {
                keys.Add(key.ToString());
            }
        }

        keys.Sort(StringComparer.Ordinal);
        return keys;
    }
}
