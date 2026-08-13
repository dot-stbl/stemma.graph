// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Abstractions.Streaming;

/// <summary>
///     Observation modes for multi-mode graph streaming.
/// </summary>
public enum StreamMode
{
    /// <summary>
    ///     Successive full (or projected) state snapshots after supersteps commit.
    /// </summary>
    Values = 0,

    /// <summary>
    ///     Per-superstep or per-node deltas (channel writes) without a full state dump.
    /// </summary>
    Updates = 1,

    /// <summary>
    ///     Lifecycle / control events (start, interrupt, end, failed, cancelled).
    /// </summary>
    Events = 2
}
