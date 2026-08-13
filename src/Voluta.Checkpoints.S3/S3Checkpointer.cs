using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoints.S3.Wire;

namespace Voluta.Checkpoints.S3;

/// <summary>
///     S3 (or S3-compatible) checkpointer: one object per (thread, step) under a key prefix.
/// </summary>
/// <remarks>
///     Key layout: <c>{prefix}/{safeThreadId}/{step:D12}.json</c>.
///     Values use System.Text.Json; prefer JSON-friendly types (strings, numbers, lists of primitives).
/// </remarks>
public sealed class S3Checkpointer(IAmazonS3 client, S3CheckpointerOptions options) : ICheckpointer
{
    private readonly string bucket = InitBucket(options);

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var document = S3CheckpointDocument.FromSnapshot(snapshot);
        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);
        var key = S3CheckpointKeys.ObjectKey(options.KeyPrefix, snapshot.ThreadId, snapshot.Step);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentBody = json,
            ContentType = "application/json",
        };

        await client.PutObjectAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var keys = await S3CheckpointListing.ListKeysAsync(
            client,
            bucket,
            S3CheckpointKeys.ThreadPrefix(options.KeyPrefix, threadId),
            cancellationToken);

        if (keys.Count == 0)
        {
            return null;
        }

        var latestKey = keys
            .Select(static key => (Key: key, Step: S3CheckpointKeys.TryParseStep(key, out var step) ? step : -1L))
            .Where(static pair => pair.Step >= 0)
            .OrderByDescending(static pair => pair.Step)
            .Select(static pair => pair.Key)
            .FirstOrDefault();

        return latestKey is null
            ? null
            : await S3CheckpointBody.GetSnapshotAsync(client, bucket, latestKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var keys = await S3CheckpointListing.ListKeysAsync(
            client,
            bucket,
            S3CheckpointKeys.ThreadPrefix(options.KeyPrefix, threadId),
            cancellationToken);

        var ordered = keys
            .Select(static key => (Key: key, Step: S3CheckpointKeys.TryParseStep(key, out var step) ? step : -1L))
            .Where(static pair => pair.Step >= 0)
            .OrderBy(static pair => pair.Step)
            .Select(static pair => pair.Key)
            .ToList();

        var list = new List<CheckpointSnapshot>(ordered.Count);
        foreach (var key in ordered)
        {
            var snapshot = await S3CheckpointBody.GetSnapshotAsync(client, bucket, key, cancellationToken);
            if (snapshot is not null)
            {
                list.Add(snapshot);
            }
        }

        return list;
    }

    private static string InitBucket(S3CheckpointerOptions checkpointerOptions)
    {
        return string.IsNullOrWhiteSpace(checkpointerOptions.BucketName)
            ? throw new ArgumentException("BucketName is required.", nameof(checkpointerOptions))
            : checkpointerOptions.BucketName;
    }
}

/// <summary>
///     List object keys under a thread prefix (paginated).
/// </summary>
file static class S3CheckpointListing
{
    public static async Task<List<string>> ListKeysAsync(
        IAmazonS3 client,
        string bucket,
        string prefix,
        CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = bucket,
                    Prefix = prefix,
                    ContinuationToken = continuationToken,
                },
                cancellationToken);

            if (response.S3Objects is not null)
            {
                foreach (var entry in response.S3Objects)
                {
                    if (!string.IsNullOrEmpty(entry.Key))
                    {
                        keys.Add(entry.Key);
                    }
                }
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return keys;
    }
}

/// <summary>
///     Download and deserialize a checkpoint object body.
/// </summary>
file static class S3CheckpointBody
{
    public static async Task<CheckpointSnapshot?> GetSnapshotAsync(
        IAmazonS3 client,
        string bucket,
        string key,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = bucket,
                Key = key,
            },
            cancellationToken);

        await using var stream = response.ResponseStream;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var document = JsonSerializer.Deserialize<S3CheckpointDocument>(json, JsonSerializerOptions.Web);
        return document?.ToSnapshot();
    }
}
