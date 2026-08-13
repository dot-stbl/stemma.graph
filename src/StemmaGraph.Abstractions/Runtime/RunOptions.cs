// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Streaming;

namespace StemmaGraph.Runtime;

/// <summary>
///     Options for invoke / stream of a compiled graph. Cancellation is a method parameter, not options.
/// </summary>
public sealed class RunOptions
{
    /// <summary>
    ///     Thread (conversation / run) id isolating checkpoints and channel state.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Preferred stream observation mode when streaming (default updates).
    /// </summary>
    public StreamMode StreamMode { get; init; } = StreamMode.Updates;
}
