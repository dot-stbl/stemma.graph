// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;

namespace StemmaGraph.State;

/// <summary>
///     Immutable channel map that can be applied to a graph builder (generated or hand-built).
/// </summary>
/// <remarks>
///     Initializes a schema from channel declarations.
/// </remarks>
/// <param name="channels">Ordered channel declarations.</param>
public sealed class GraphChannelSchema(IReadOnlyList<GraphChannelDeclaration> channels)
{
    /// <summary>
    ///     Channel declarations in registration order.
    /// </summary>
    public IReadOnlyList<GraphChannelDeclaration> Channels { get; } = channels;

    /// <summary>
    ///     Fluent builder that mirrors the shape source generation emits.
    /// </summary>
    public sealed class Builder
    {
        private readonly List<GraphChannelDeclaration> channels = [];

        /// <summary>
        ///     Adds a channel declaration.
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
        ///     Builds an immutable schema.
        /// </summary>
        /// <returns>Schema ready for <c>StateGraph.AddChannels</c>.</returns>
        public GraphChannelSchema Build()
        {
            return new GraphChannelSchema([.. channels]);
        }
    }
}
