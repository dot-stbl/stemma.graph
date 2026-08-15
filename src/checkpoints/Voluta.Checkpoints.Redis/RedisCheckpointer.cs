using System.Text.Json;
using StackExchange.Redis;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Checkpoints.Redis.Wire;

namespace Voluta.Checkpoints.Redis;

/// <summary>
///     Redis checkpointer: one sorted set per thread (score = step, member = wire JSON).
/// </summary>
/// <remarks>
///     Key layout: <c>{prefix}thread:{safeThreadId}</c>.
///     Host registration: <c>v.Checkpoints.UseRedis(configure)</c> — requires
///     <see cref="IConnectionMultiplexer" /> registered in DI.
///     Direct construction is internal for conformance / unit tests only.
///     Channel values must be wire-format v1 allow-listed shapes; unsupported types fail Put with
///     <c>checkpoint.unsupported_value_type</c>.
///     Implements <see cref="IThreadDiscovery" /> via key scanning under the prefix.
/// </remarks>
public sealed class RedisCheckpointer : ICheckpointer, IThreadDiscovery
{
    private readonly IRedisCheckpointStore store;
    private readonly RedisCheckpointerOptions options;

    /// <summary>
    ///     Creates a Redis checkpointer over a live connection.
    /// </summary>
    /// <param name="multiplexer">Redis connection multiplexer.</param>
    /// <param name="options">Key prefix / database options.</param>
    internal RedisCheckpointer(IConnectionMultiplexer multiplexer, RedisCheckpointerOptions options)
        : this(new RedisCheckpointStore(multiplexer, options.Database), options)
    {
    }

    /// <summary>
    ///     Creates a Redis checkpointer over a substituted store (tests).
    /// </summary>
    /// <param name="store">Storage seam implementation.</param>
    /// <param name="options">Key prefix options.</param>
    internal RedisCheckpointer(IRedisCheckpointStore store, RedisCheckpointerOptions options)
    {
        this.store = store;
        this.options = options;
    }

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = RedisCheckpointDocument.FromSnapshot(snapshot);
            var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);
            await store.PutAsync(
                RedisCheckpointKeys.ThreadKey(options.KeyPrefix, snapshot.ThreadId),
                snapshot.Step,
                json,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointPutFailed,
                $"Failed to put checkpoint for thread '{snapshot.ThreadId}' step {snapshot.Step}.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latest = await store.LatestAsync(
                RedisCheckpointKeys.ThreadKey(options.KeyPrefix, threadId),
                cancellationToken);
            return latest is { } entry ? Deserialize(entry.Json) : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointGetFailed,
                $"Failed to get checkpoint for thread '{threadId}'.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await store.ListAsync(
                RedisCheckpointKeys.ThreadKey(options.KeyPrefix, threadId),
                cancellationToken);

            var snapshots = new List<CheckpointSnapshot>(entries.Count);
            foreach (var entry in entries.OrderBy(static item => item.Step))
            {
                if (TryDeserialize(entry.Json) is { } snapshot)
                {
                    snapshots.Add(snapshot);
                }
            }

            return snapshots;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointListFailed,
                $"Failed to list checkpoints for thread '{threadId}'.",
                exception);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Scans keys matching <c>{prefix}thread:*</c>; thread ids are the sanitized
    ///     segments used on Put.
    /// </remarks>
    public async Task<IReadOnlyList<string>> ListThreadIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = await store.ListKeysAsync(
                RedisCheckpointKeys.ThreadScanPattern(options.KeyPrefix),
                cancellationToken);

            var ids = new List<string>();
            foreach (var key in keys)
            {
                if (RedisCheckpointKeys.TryParseThreadIdFromKey(key, options.KeyPrefix) is { } threadId)
                {
                    ids.Add(threadId);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointListFailed,
                "Failed to list thread identifiers from the Redis checkpoint store.",
                exception);
        }
    }

    private static CheckpointSnapshot? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<RedisCheckpointDocument>(json, JsonSerializerOptions.Web)
                ?.ToSnapshot();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CheckpointSnapshot Deserialize(string json)
    {
        return TryDeserialize(json)
            ?? throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointCorruptPayload,
                "Redis checkpoint member could not be deserialized as a checkpoint.");
    }
}
