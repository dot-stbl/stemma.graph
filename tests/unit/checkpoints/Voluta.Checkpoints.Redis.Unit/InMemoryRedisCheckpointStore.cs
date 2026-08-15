namespace Voluta.Checkpoints.Redis;

/// <summary>
///     In-memory <see cref="IRedisCheckpointStore" /> double for unit tests.
/// </summary>
internal sealed class InMemoryRedisCheckpointStore : IRedisCheckpointStore
{
    private readonly Dictionary<string, SortedDictionary<long, string>> sets = new(StringComparer.Ordinal);

    public Task PutAsync(string threadKey, long step, string json, CancellationToken cancellationToken)
    {
        if (!sets.TryGetValue(threadKey, out var set))
        {
            set = new SortedDictionary<long, string>();
            sets[threadKey] = set;
        }

        set[step] = json;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RedisCheckpointEntry>> ListAsync(
        string threadKey,
        CancellationToken cancellationToken)
    {
        if (!sets.TryGetValue(threadKey, out var set))
        {
            return Task.FromResult<IReadOnlyList<RedisCheckpointEntry>>([]);
        }

        var entries = set
            .Select(static pair => new RedisCheckpointEntry(pair.Key, pair.Value))
            .ToArray();
        return Task.FromResult<IReadOnlyList<RedisCheckpointEntry>>(entries);
    }

    public Task<RedisCheckpointEntry?> LatestAsync(
        string threadKey,
        CancellationToken cancellationToken)
    {
        if (!sets.TryGetValue(threadKey, out var set) || set.Count == 0)
        {
            return Task.FromResult<RedisCheckpointEntry?>(null);
        }

        var last = set.Last();
        return Task.FromResult<RedisCheckpointEntry?>(new RedisCheckpointEntry(last.Key, last.Value));
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string pattern, CancellationToken cancellationToken)
    {
        var prefix = pattern.EndsWith("*", StringComparison.Ordinal)
            ? pattern[..^1]
            : pattern;
        var keys = sets.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<string>>(keys);
    }
}
