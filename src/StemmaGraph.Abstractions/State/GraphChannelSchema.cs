// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;

namespace StemmaGraph.State;

/// <summary>
/// Immutable channel map that can be applied to a graph builder (generated or hand-built).
/// </summary>
public sealed class GraphChannelSchema
{
    /// <summary>
    /// Initializes a schema from channel declarations.
    /// </summary>
    /// <param name="channels">Ordered channel declarations.</param>
    public GraphChannelSchema(IReadOnlyList<GraphChannelDeclaration> channels)
    {
        Channels = channels;
    }

    /// <summary>
    /// Channel declarations in registration order.
    /// </summary>
    public IReadOnlyList<GraphChannelDeclaration> Channels { get; }

    /// <summary>
    /// Fluent builder that mirrors the shape source generation emits.
    /// </summary>
    public sealed class Builder
    {
        private readonly List<GraphChannelDeclaration> channels = [];

        /// <summary>
        /// Adds a channel declaration.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="kind">Merge kind.</param>
        /// <returns>This builder for chaining.</returns>
        public Builder Add(string name, ChannelKind kind)
        {
            channels.Add(new GraphChannelDeclaration(name, kind));
            return this;
        }

        /// <summary>
        /// Builds an immutable schema.
        /// </summary>
        /// <returns>Schema ready for <c>StateGraph.AddChannels</c>.</returns>
        public GraphChannelSchema Build()
        {
            return new GraphChannelSchema(channels.ToArray());
        }
    }
}