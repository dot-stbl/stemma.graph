namespace Voluta.Checkpoints.Redis;

/// <summary>
///     Thin storage seam over Redis sorted sets — real implementation talks to
///     StackExchange.Redis; unit tests substitute an in-memory store.
/// </summary>
/// <remarks>
///     Each thread is one sorted-set key: score = checkpoint step, member = wire JSON.
///     Put replaces any existing member with the same score (last write wins per step).
/// </remarks>
internal interface IRedisCheckpointStore
{
    /// <summary>Upserts the JSON member under the given step score (removing same-score leftovers first).</summary>
    public Task PutAsync(string threadKey, long step, string json, CancellationToken cancellationToken);

    /// <summary>Returns all members ordered by step ascending; empty when the key is absent.</summary>
    public Task<IReadOnlyList<RedisCheckpointEntry>> ListAsync(string threadKey, CancellationToken cancellationToken);

    /// <summary>Returns the highest-step member or <see langword="null" /> when the key is absent.</summary>
    public Task<RedisCheckpointEntry?> LatestAsync(string threadKey, CancellationToken cancellationToken);

    /// <summary>SCAN-style key listing matching the given pattern (thread discovery).</summary>
    public Task<IReadOnlyList<string>> ListKeysAsync(string pattern, CancellationToken cancellationToken);
}

/// <summary>One (step, json) pair read back from a thread sorted set.</summary>
/// <param name="Step">Checkpoint step (sorted-set score).</param>
/// <param name="Json">Wire-format JSON member.</param>
internal readonly record struct RedisCheckpointEntry(long Step, string Json);
