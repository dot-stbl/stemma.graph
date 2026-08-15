namespace Voluta.Checkpoints.Redis;

/// <summary>
///     Configuration for <see cref="RedisCheckpointer" />.
/// </summary>
public sealed class RedisCheckpointerOptions
{
    /// <summary>
    ///     Key prefix for all checkpoint keys (default <c>voluta:</c>).
    ///     Thread sets are stored as <c>{prefix}thread:{safeThreadId}</c>.
    /// </summary>
    public string KeyPrefix { get; init; } = "voluta:";

    /// <summary>Redis database number (default 0).</summary>
    public int Database { get; init; }
}
