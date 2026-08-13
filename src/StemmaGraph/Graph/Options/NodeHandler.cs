// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Graph;
using StemmaGraph.Results;

namespace StemmaGraph;

/// <summary>
/// Async node body: reads a frozen superstep view and returns continue or interrupt.
/// </summary>
/// <param name="context">Frozen channel snapshot and resume payload for this task.</param>
/// <param name="cancellationToken">Cooperative cancellation from the host stream/invoke.</param>
/// <returns>Continue with partial writes, or interrupt for HITL.</returns>
public delegate Task<NodeResult> NodeHandler(GraphContext context, CancellationToken cancellationToken);
