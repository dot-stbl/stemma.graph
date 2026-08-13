// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Runtime.Engine;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;

namespace StemmaGraph.Graph;

/// <summary>
///     Immutable compiled graph: stream, invoke, and resume against a checkpointer.
/// </summary>
public sealed class CompiledGraph
{
    private readonly ICheckpointer checkpointer;
    private readonly GraphTopology topology;

    /// <summary>
    ///     Initializes a compiled graph from validated topology.
    /// </summary>
    /// <param name="topology">Immutable topology.</param>
    /// <param name="checkpointer">Checkpoint provider for this runnable.</param>
    internal CompiledGraph(GraphTopology topology, ICheckpointer checkpointer)
    {
        this.topology = topology;
        this.checkpointer = checkpointer;
    }

    /// <summary>
    ///     Streams multi-mode observation items until the run reaches a terminal status.
    /// </summary>
    /// <param name="input">Initial channel writes (seed).</param>
    /// <param name="options">Thread id and stream mode.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Async sequence of stream events.</returns>
    public IAsyncEnumerable<StreamEvent> StreamAsync(
        IEnumerable<ChannelWrite> input,
        RunOptions options,
        CancellationToken cancellationToken = default)
    {
        var engine = new RunEngine(topology, checkpointer);
        return engine.StreamAsync(input, options, cancellationToken);
    }

    /// <summary>
    ///     Drains the stream to completion and returns the last terminal event.
    /// </summary>
    /// <param name="input">Initial channel writes (seed).</param>
    /// <param name="options">Thread id and preferred observation mode.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Final stream event (end, interrupt, failed, or cancelled).</returns>
    public async Task<StreamEvent> InvokeAsync(
        IEnumerable<ChannelWrite> input,
        RunOptions options,
        CancellationToken cancellationToken = default)
    {
        return await DrainToTerminalAsync(
            StreamAsync(input, options, cancellationToken),
            options.StreamMode);
    }

    /// <summary>
    ///     Resumes an interrupted thread with a command.
    /// </summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="command">Resume payload / values.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Async sequence continuing from the interrupted checkpoint.</returns>
    public IAsyncEnumerable<StreamEvent> ResumeAsync(
        string threadId,
        Command command,
        CancellationToken cancellationToken = default)
    {
        return ResumeAsync(threadId, command, StreamMode.Updates, cancellationToken);
    }

    /// <summary>
    ///     Resumes an interrupted thread with a command and stream mode.
    /// </summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="command">Resume payload / values.</param>
    /// <param name="streamMode">Observation mode for the resumed stream.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Async sequence continuing from the interrupted checkpoint.</returns>
    public IAsyncEnumerable<StreamEvent> ResumeAsync(
        string threadId,
        Command command,
        StreamMode streamMode,
        CancellationToken cancellationToken = default)
    {
        var engine = new RunEngine(topology, checkpointer);
        return engine.ResumeAsync(threadId, command, streamMode, cancellationToken);
    }

    /// <summary>
    ///     Drains a resume stream to the next terminal event.
    /// </summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="command">Resume payload / values.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Final stream event after resume.</returns>
    public async Task<StreamEvent> ResumeInvokeAsync(
        string threadId,
        Command command,
        CancellationToken cancellationToken = default)
    {
        return await DrainToTerminalAsync(
            ResumeAsync(threadId, command, StreamMode.Updates, cancellationToken),
            StreamMode.Updates);
    }

    private static async Task<StreamEvent> DrainToTerminalAsync(
        IAsyncEnumerable<StreamEvent> stream,
        StreamMode streamMode)
    {
        StreamEvent? last = null;
        await foreach (var item in stream)
        {
            last = item;
            if (item.Kind is StreamEventKind.Failed && item.Payload is Exception exception)
            {
                throw exception;
            }

            if (item.Kind is StreamEventKind.End
                or StreamEventKind.Interrupt
                or StreamEventKind.Failed
                or StreamEventKind.Cancelled)
            {
                return item;
            }
        }

        return last ?? new StreamEvent
        {
            Mode = streamMode,
            Kind = StreamEventKind.End
        };
    }
}
