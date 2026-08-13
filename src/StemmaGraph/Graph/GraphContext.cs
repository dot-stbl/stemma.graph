// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Graph;

/// <summary>
/// Frozen superstep view passed to a node handler (pre-barrier channel values).
/// </summary>
public sealed class GraphContext
{
    private readonly IReadOnlyDictionary<string, object?> channelValues;

    /// <summary>
    /// Initializes a graph context for one node task.
    /// </summary>
    /// <param name="nodeName">Node being executed.</param>
    /// <param name="channelValues">Snapshot of channel values before this superstep's apply.</param>
    /// <param name="resumePayload">Resume command payload when continuing after interrupt.</param>
    public GraphContext(
        string nodeName,
        IReadOnlyDictionary<string, object?> channelValues,
        object? resumePayload = null)
    {
        NodeName = nodeName;
        this.channelValues = channelValues;
        ResumePayload = resumePayload;
    }

    /// <summary>
    /// Name of the node currently executing.
    /// </summary>
    public string NodeName { get; }

    /// <summary>
    /// Resume command payload when this invocation is a resume of an interrupted node.
    /// </summary>
    public object? ResumePayload { get; }

    /// <summary>
    /// Reads a channel value cast to <typeparamref name="T"/>, or default when missing/null.
    /// </summary>
    /// <typeparam name="T">Expected value type.</typeparam>
    /// <param name="channelName">Channel name.</param>
    /// <returns>Channel value or default.</returns>
    public T? Read<T>(string channelName)
    {
        return !channelValues.TryGetValue(channelName, out var value) || value is null
            ? default
            : value is T typed ? typed : (T)value;
    }

    /// <summary>
    /// Returns the full frozen channel map for this superstep.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        return channelValues;
    }
}
