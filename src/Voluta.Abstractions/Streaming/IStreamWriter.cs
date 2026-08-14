namespace Voluta.Abstractions.Streaming;

/// <summary>
///     Writes custom progress and LLM token fragments into the graph stream while a node runs.
///     Injected on <c>GraphContext.Stream</c> by the runtime; no-op when absent.
/// </summary>
public interface IStreamWriter
{
    /// <summary>
    ///     Emits a custom progress / structured payload event (<see cref="StreamEventKind.Custom" />).
    /// </summary>
    /// <param name="payload">Opaque payload (string, DTO, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask WriteCustomAsync(object? payload, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Emits an LLM token / message fragment (<see cref="StreamEventKind.Messages" />).
    /// </summary>
    /// <param name="text">Token or text delta.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask WriteMessageAsync(string text, CancellationToken cancellationToken = default);
}
