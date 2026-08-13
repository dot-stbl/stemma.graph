// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.Results;

/// <summary>
///     Outcome of a node task: continue with partial writes, or interrupt for HITL.
///     Interrupt is expressed as a result, not as a control-flow exception.
/// </summary>
public abstract class NodeResult
{
    private protected NodeResult()
    {
    }

    /// <summary>
    ///     Creates a continue result with the given partial channel writes.
    /// </summary>
    /// <param name="writes">Partial channel updates; may be empty.</param>
    /// <returns>A continue result the runtime can apply and schedule from.</returns>
    public static ContinueNodeResult Continue(IReadOnlyList<ChannelWrite> writes)
    {
        return new ContinueNodeResult(writes);
    }

    /// <summary>
    ///     Creates a continue result with zero or more partial channel writes.
    /// </summary>
    /// <param name="writes">Partial channel updates.</param>
    /// <returns>A continue result the runtime can apply and schedule from.</returns>
    public static ContinueNodeResult Continue(params ChannelWrite[] writes)
    {
        return new ContinueNodeResult(writes);
    }

    /// <summary>
    ///     Creates an interrupt result that pauses the run with a serializable payload.
    /// </summary>
    /// <param name="payload">HITL payload stored on the interrupted checkpoint.</param>
    /// <returns>An interrupt result; no further supersteps run until resume.</returns>
    public static InterruptNodeResult Interrupt(object? payload)
    {
        return new InterruptNodeResult(payload);
    }
}
