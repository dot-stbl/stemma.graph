// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Abstractions.Results;

/// <summary>
///     HITL pause: run status becomes interrupted and a checkpoint records the payload.
/// </summary>
/// <remarks>
///     Initializes an interrupt result.
/// </remarks>
/// <param name="payload">Serializable interrupt payload for the host / UI.</param>
public sealed class InterruptNodeResult(object? payload) : NodeResult
{
    /// <summary>
    ///     Interrupt payload persisted on the checkpoint and returned to the host.
    /// </summary>
    public object? Payload { get; } = payload;
}
