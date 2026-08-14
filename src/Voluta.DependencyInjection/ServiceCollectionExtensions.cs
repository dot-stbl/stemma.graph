using Microsoft.Extensions.DependencyInjection;
using Voluta.DependencyInjection.Checkpoints;
using Voluta.Graph;

namespace Voluta.DependencyInjection;

/// <summary>
///     DI registration helpers for Voluta (graph + checkpoints).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Configures Voluta via a fluent builder: checkpoints and/or compiled graph.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Builder configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    ///     <code>
    ///     services.AddVoluta(v =>
    ///     {
    ///         v.Checkpoints.UseInMemory();
    ///         v.Graph((sp, checkpointer) => new StateGraph()
    ///             .AddNode("a", …)
    ///             .AddEdge(GraphConstants.Start, "a")
    ///             .AddEdge("a", GraphConstants.End)
    ///             .Compile(checkpointer));
    ///     });
    ///     </code>
    /// </example>
    public static IServiceCollection AddVoluta(
        this IServiceCollection services,
        Action<VolutaBuilder> configure)
    {
        var builder = new VolutaBuilder(services);
        configure(builder);
        builder.Complete();
        return services;
    }

    /// <summary>
    ///     Registers an already-compiled graph as a singleton (no checkpoint registration).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="graph">Compiled graph instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoluta(this IServiceCollection services, CompiledGraph graph)
    {
        return services.AddVoluta(builder => builder.Graph(graph));
    }

    /// <summary>
    ///     Compiles a graph via factory and registers it as a singleton (no checkpoint registration).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="factory">Factory that builds and compiles the graph once.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoluta(
        this IServiceCollection services,
        Func<IServiceProvider, CompiledGraph> factory)
    {
        return services.AddVoluta(builder => builder.Graph(factory));
    }

    /// <summary>
    ///     Configures only the process-wide <see cref="Abstractions.Checkpoint.ICheckpointer" />.
    ///     Prefer <see cref="AddVoluta(IServiceCollection, Action{VolutaBuilder})" /> when also registering a graph.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Provider selection (e.g. <c>c =&gt; c.UseInMemory()</c>).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no <c>Use*</c> was called, or more than one was called.
    /// </exception>
    public static IServiceCollection AddVolutaCheckpoints(
        this IServiceCollection services,
        Action<VolutaCheckpointBuilder> configure)
    {
        return services.AddVoluta(builder =>
        {
            configure(builder.Checkpoints);
            if (!builder.Checkpoints.IsProviderConfigured)
            {
                throw new InvalidOperationException(
                    "AddVolutaCheckpoints requires exactly one Use* provider (UseInMemory, UseFile, UseSqlite, UseEntityFrameworkCore, UseS3).");
                    "AddVolutaCheckpoints requires exactly one Use* provider (UseInMemory, UseFile, UseEntityFrameworkCore, UseS3, UsePostgres).");
            }
        });
    }

    /// <summary>
    ///     Configures only the process-wide <see cref="Abstractions.Store.IVolutaStore" />
    ///     (cross-thread KV, independent of checkpoints).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Provider selection (e.g. <c>s =&gt; s.UseInMemory()</c>).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no <c>Use*</c> was called, or more than one was called.
    /// </exception>
    public static IServiceCollection AddVolutaStore(
        this IServiceCollection services,
        Action<Store.VolutaStoreBuilder> configure)
    {
        return services.AddVoluta(builder =>
        {
            configure(builder.Store);
            if (!builder.Store.IsProviderConfigured)
            {
                throw new InvalidOperationException(
                    "AddVolutaStore requires exactly one Use* provider (UseInMemory).");
            }
        });
    }
}
