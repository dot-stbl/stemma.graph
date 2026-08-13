// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Microsoft.Extensions.DependencyInjection;
using StemmaGraph.Graph;

namespace StemmaGraph.Hosting;

/// <summary>
///     Thin DI helpers for registering a compiled graph as a singleton.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers an already-compiled graph as a singleton.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="graph">Compiled graph instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStemmaGraph(this IServiceCollection services, CompiledGraph graph)
    {
        _ = services.AddSingleton(graph);
        return services;
    }

    /// <summary>
    ///     Compiles a graph via factory and registers it as a singleton.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="factory">Factory that builds and compiles the graph once.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStemmaGraph(
        this IServiceCollection services,
        Func<IServiceProvider, CompiledGraph> factory)
    {
        _ = services.AddSingleton(factory);
        return services;
    }
}
