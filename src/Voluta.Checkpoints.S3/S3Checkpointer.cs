using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Checkpoints.S3.Wire;

namespace Voluta.Checkpoints.S3;

/// <summary>
///     S3 (or S3-compatible) checkpointer: one object per (thread, step) under a key prefix.
/// </summary>
/// <remarks>
///     Key layout: <c>{prefix}/{safeThreadId}/{step:D12}.json</c>.
///     Host registration: <c>v.Checkpoints.UseS3(configure)</c>.
///     Direct construction is internal for conformance / unit tests only.
    ///     Channel values must be wire-format v1 allow-listed shapes; unsupported types fail Put with
    ///     <c>checkpoint.unsupported_value_type</c>.
/// </remarks>
public sealed class S3Checkpointer : ICheckpointer
{
    private readonly IAmazonS3 client;
    private readonly S3CheckpointerOptions options;
    private readonly string bucket;

    /// <summary>
    ///     Creates an S3 checkpointer.
    /// </summary>
    /// <param name="client">S3 client (must already be configured for the target endpoint).</param>
    /// <param name="options">Bucket and key-prefix options.</param>
    internal S3Checkpointer(IAmazonS3 client, S3CheckpointerOptions options)
    {
        this.client = client;
        this.options = options;
        bucket = InitBucket(options);
    }

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = S3CheckpointDocument.FromSnapshot(snapshot);
            var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);
            var key = S3CheckpointKeys.ObjectKey(options.KeyPrefix, snapshot.ThreadId, snapshot.Step);

            await client.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    ContentBody = json,
                    ContentType = "application/json",
                },
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
            var keys = await S3CheckpointListing.ListKeysAsync(
                client,
                bucket,
                S3CheckpointKeys.ThreadPrefix(options.KeyPrefix, threadId),
                cancellationToken);

            var orderedKeys = keys
                .Select(static key => (Key: key, Step: S3CheckpointKeys.TryParseStep(key, out var step) ? step : -1L))
                .Where(static pair => pair.Step >= 0)
                .OrderBy(static pair => pair.Step)
                .Select(static pair => pair.Key)
                .ToArray();

            if (orderedKeys.Length == 0)
            {
                return [];
            }

            var snapshots = new CheckpointSnapshot[orderedKeys.Length];
            for (var index = 0; index < orderedKeys.Length; index++)
            {
                var snapshot = await S3CheckpointBody.GetSnapshotAsync(
                    client,
                    bucket,
                    orderedKeys[index],
                    cancellationToken);
                snapshots[index] = snapshot
                    ?? throw new CheckpointStoreException(
                        VolutaErrorCodes.CheckpointCorruptPayload,
                        $"S3 object '{orderedKeys[index]}' could not be deserialized as a checkpoint.");
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
    public static async Task<IReadOnlyList<string>> ListKeysAsync(
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
                keys.AddRange(
                    response.S3Objects
                        .Select(static entry => entry.Key)
                        .Where(static key => !string.IsNullOrEmpty(key))!);
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
