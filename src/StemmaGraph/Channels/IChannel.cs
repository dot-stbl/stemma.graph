// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Channels;

/// <summary>
///     Internal channel storage with superstep multi-write update and checkpoint restore.
/// </summary>
internal interface IChannel
{
    /// <summary>
    ///     Declared merge kind for this channel.
    /// </summary>
    public ChannelKind Kind { get; }

    /// <summary>
    ///     Current channel value after the last successful apply.
    /// </summary>
    public object? Get();

    /// <summary>
    ///     Applies all writes for one superstep in deterministic order.
    /// </summary>
    /// <param name="values">Ordered write values for this channel in the superstep.</param>
    public void Update(IReadOnlyList<object?> values);

    /// <summary>
    ///     Restores the channel value from a checkpoint without versioning side effects.
    /// </summary>
    /// <param name="value">Checkpointed value.</param>
    public void Restore(object? value);
}

/// <summary>
///     Creates channel instances from declared kinds.
/// </summary>
internal static class ChannelFactory
{
    /// <summary>
    ///     Creates a fresh channel for the given kind.
    /// </summary>
    /// <param name="kind">Declared channel kind.</param>
    /// <returns>A new channel instance.</returns>
    public static IChannel Create(ChannelKind kind)
    {
        return kind switch
        {
            ChannelKind.LastValue => new LastValueChannel(),
            ChannelKind.Append => new AppendChannel(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown channel kind.")
        };
    }
}
