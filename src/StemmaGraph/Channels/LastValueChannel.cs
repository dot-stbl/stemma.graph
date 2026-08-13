// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;
using StemmaGraph.Runtime.Exceptions;

namespace StemmaGraph.Runtime.Channels;

/// <summary>
///     Channel that accepts at most one write per superstep.
/// </summary>
internal sealed class LastValueChannel : IChannel
{
    private object? value;

    /// <inheritdoc />
    public ChannelKind Kind => ChannelKind.LastValue;

    /// <inheritdoc />
    public object? Get()
    {
        return value;
    }

    /// <inheritdoc />
    public void Update(IReadOnlyList<object?> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        if (values.Count > 1)
        {
            throw new GraphConcurrentUpdateException(
                "LastValue channel received more than one write in a single superstep.");
        }

        value = values[0];
    }

    /// <inheritdoc />
    public void Restore(object? restored)
    {
        value = restored;
    }
}
