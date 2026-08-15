namespace Voluta.Checkpoints.S3;

/// <summary>
///     Configuration for <see cref="S3Checkpointer" />.
/// </summary>
public sealed class S3CheckpointerOptions
{
    /// <summary>Target S3 bucket name (required).</summary>
    public required string BucketName { get; init; }

    /// <summary>
    ///     Optional key prefix (no leading/trailing slash required). Objects are stored as
    ///     <c>{prefix}/{safeThreadId}/{step:D12}.json</c>.
    /// </summary>
    public string? KeyPrefix { get; init; }
}
