// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Graph;

/// <summary>
///     Frozen superstep view passed to a node handler (pre-barrier channel values).
/// </summary>
/// <remarks>
///     Initializes a graph context for one node task.
/// </remarks>
/// <param name="nodeName">Node being executed.</param>
/// <param name="channelValues">Snapshot of channel values before this superstep's apply.</param>
/// <param name="resumePayload">Resume command payload when continuing after interrupt.</param>
public sealed class GraphContext(
    string nodeName,
    IReadOnlyDictionary<string, object?> channelValues,
    object? resumePayload = null)
{
    private readonly IReadOnlyDictionary<string, object?> channelValues = channelValues;

    /// <summary>
    ///     Name of the node currently executing.
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    ///     Resume command payload when this invocation is a resume of an interrupted node.
    /// </summary>
    public object? ResumePayload { get; } = resumePayload;

    /// <summary>
    ///     Reads a channel value cast to <typeparamref name="T" />, or default when missing/null.
    /// </summary>
    /// <typeparam name="T">Expected value type.</typeparam>
    /// <param name="channelName">Channel name.</param>
    /// <returns>Channel value or default.</returns>
    public T? Read<T>(string channelName)
    {
        return !channelValues.TryGetValue(channelName, out var value) || value is null
            ? default
            : value is T typed
                ? typed
                : (T)value;
    }

    /// <summary>
    ///     Returns the full frozen channel map for this superstep.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        return channelValues;
    }
}
