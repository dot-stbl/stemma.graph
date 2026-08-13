// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Results;

/// <summary>
/// HITL pause: run status becomes interrupted and a checkpoint records the payload.
/// </summary>
public sealed class InterruptNodeResult : NodeResult
{
    /// <summary>
    /// Initializes an interrupt result.
    /// </summary>
    /// <param name="payload">Serializable interrupt payload for the host / UI.</param>
    public InterruptNodeResult(object? payload)
    {
        Payload = payload;
    }

    /// <summary>
    /// Interrupt payload persisted on the checkpoint and returned to the host.
    /// </summary>
    public object? Payload { get; }
}
