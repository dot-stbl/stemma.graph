// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using System.Collections;
using StemmaGraph.Channels;

namespace StemmaGraph.Runtime.Channels;

/// <summary>
///     Channel that merges multiple superstep writes into an ordered list.
/// </summary>
internal sealed class AppendChannel : IChannel
{
    private List<object?> items = [];

    /// <inheritdoc />
    public ChannelKind Kind => ChannelKind.Append;

    /// <inheritdoc />
    public object? Get()
    {
        return items.ToList();
    }

    /// <inheritdoc />
    public void Update(IReadOnlyList<object?> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        foreach (var write in values)
        {
            AppendChannelHelpers.AppendValue(items, write);
        }
    }

    /// <inheritdoc />
    public void Restore(object? restored)
    {
        items = AppendChannelHelpers.MaterializeList(restored);
    }
}

/// <summary>
///     Pure helpers for append-channel merge semantics.
/// </summary>
file static class AppendChannelHelpers
{
    public static void AppendValue(List<object?> items, object? write)
    {
        if (write is null)
        {
            items.Add(null);
            return;
        }

        if (write is string)
        {
            items.Add(write);
            return;
        }

        if (write is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                items.Add(item);
            }

            return;
        }

        items.Add(write);
    }

    public static List<object?> MaterializeList(object? restored)
    {
        if (restored is null)
        {
            return [];
        }

        if (restored is IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            return list;
        }

        return [restored];
    }
}
