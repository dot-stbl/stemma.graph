using Microsoft.Extensions.Logging;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Exceptions;

namespace Voluta.Diagnostics;

/// <summary>
///     Host helpers: map stable exception codes to catalog <see cref="EventId" /> values
///     for structured logging. Prefer logging the full exception + code + EventId;
///     never put secrets or PII in <see cref="Exception.Message" />.
/// </summary>
public static class VolutaExceptionLogging
{
    private static readonly Dictionary<string, EventId> ByCode =
        VolutaEventIds.All.ToDictionary(
            static eventId => eventId.Name ?? string.Empty,
            static eventId => eventId,
            StringComparer.Ordinal);

    /// <summary>
    ///     Resolves the catalog EventId for a known code, or a fallback id when the code is unknown.
    /// </summary>
    /// <param name="code">Stable dot.case code from <see cref="VolutaErrorCodes" />.</param>
    /// <returns>Matching catalog EventId, or id <c>2999</c> named with the raw code.</returns>
    public static EventId GetEventId(string code)
    {
        return string.IsNullOrEmpty(code)
            ? new EventId(2999, "voluta.unknown")
            : ByCode.TryGetValue(code, out var eventId)
                ? eventId
                : new EventId(2999, code);
    }

    /// <summary>
    ///     Resolves the EventId for a <see cref="GraphException" /> via its <see cref="GraphException.Code" />.
    /// </summary>
    /// <param name="exception">Graph runtime or compile failure.</param>
    /// <returns>Catalog or fallback EventId.</returns>
    public static EventId GetEventId(GraphException exception)
    {
        return GetEventId(exception.Code);
    }

    /// <summary>
    ///     Resolves the EventId for a <see cref="CheckpointStoreException" /> via its code.
    /// </summary>
    /// <param name="exception">Checkpoint storage failure.</param>
    /// <returns>Catalog or fallback EventId.</returns>
    public static EventId GetEventId(CheckpointStoreException exception)
    {
        return GetEventId(exception.Code);
    }
}
