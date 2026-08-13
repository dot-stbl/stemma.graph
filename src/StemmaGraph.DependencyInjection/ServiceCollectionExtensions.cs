using Microsoft.Extensions.DependencyInjection;
using StemmaGraph.Graph;

namespace StemmaGraph.DependencyInjection;

/// <summary>
///     DI registration helpers for a compiled StemmaGraph.
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
        services.AddSingleton(graph);
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
        services.AddSingleton(factory);
        return services;
    }
}
