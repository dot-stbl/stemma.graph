namespace Voluta.Exceptions;

/// <summary>
///     Base type for graph runtime and compile failures with a stable machine code.
/// </summary>
/// <remarks>
///     Initializes a graph exception. Prefer codes from
///     <see cref="Abstractions.Diagnostics.VolutaErrorCodes" />; hosts resolve MEL
///     EventIds via <c>Voluta.Diagnostics.VolutaExceptionLogging</c>.
/// </remarks>
/// <param name="code">Stable dot.case error code for host branching.</param>
/// <param name="message">Safe human message (no secrets / PII).</param>
/// <param name="innerException">Optional inner exception.</param>
public class GraphException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>
    ///     Stable machine-readable error code (see <see cref="Abstractions.Diagnostics.VolutaErrorCodes" />).
    /// </summary>
    public string Code { get; } = code;
}
