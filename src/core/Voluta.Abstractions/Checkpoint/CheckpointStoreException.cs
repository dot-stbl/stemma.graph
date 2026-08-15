namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     Storage failure from an <see cref="ICheckpointer" /> implementation (not a graph logic error).
///     Hosts branch on <see cref="Code" />; miss-on-Get remains null, not this exception.
/// </summary>
/// <param name="code">
///     Stable dot.case code from <see cref="Diagnostics.VolutaErrorCodes" />
///     (e.g. <c>checkpoint.put_failed</c>).
/// </param>
/// <param name="message">Safe human message (no secrets / connection strings).</param>
/// <param name="innerException">Provider exception (EF, S3, IO).</param>
public sealed class CheckpointStoreException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>Stable machine-readable error code (see <see cref="Diagnostics.VolutaErrorCodes" />).</summary>
    public string Code { get; } = code;
}
