// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Streaming;

namespace StemmaGraph.Testing.Streaming;

/// <summary>
///     Collects <see cref="StreamEvent" /> items from an async stream for offline assertions.
/// </summary>
public sealed class StreamCapture
{
    private StreamCapture(IReadOnlyList<StreamEvent> events)
    {
        Events = events;
    }

    /// <summary>
    ///     Captured events in emission order.
    /// </summary>
    public IReadOnlyList<StreamEvent> Events { get; }

    /// <summary>
    ///     Drains <paramref name="stream" /> into a capture bag.
    /// </summary>
    /// <param name="stream">Graph stream (values, updates, or events).</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Capture with all events observed before completion or cancellation.</returns>
    public static async Task<StreamCapture> CollectAsync(
        IAsyncEnumerable<StreamEvent> stream,
        CancellationToken cancellationToken = default)
    {
        var events = new List<StreamEvent>();
        await foreach (var streamEvent in stream.WithCancellation(cancellationToken))
        {
            events.Add(streamEvent);
        }

        return new StreamCapture(events);
    }
}
