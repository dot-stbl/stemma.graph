using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;
using Voluta.DependencyInjection.Store;
using Voluta.Graph;

namespace Voluta.DependencyInjection;

/// <summary>
///     Fluent composition root for Voluta host registration: checkpoints, cross-thread store, graph.
/// </summary>
public sealed class VolutaBuilder(IServiceCollection services)
{
    private Func<IServiceProvider, CompiledGraph>? graphFactory;
    private bool graphRequiresCheckpointer;

    /// <summary>Service collection being configured.</summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    ///     Checkpoint store registration (<c>UseInMemory</c> / <c>UseFile</c> / EF / S3).
    /// </summary>
    public VolutaCheckpointBuilder Checkpoints { get; } = new(services);

    /// <summary>
    ///     Cross-thread key-value store registration (<c>UseInMemory</c>). Optional — independent of checkpoints.
    /// </summary>
    public VolutaStoreBuilder Store { get; } = new(services);

    /// <summary>
    ///     Registers a pre-built <see cref="CompiledGraph" /> as a singleton.
    ///     Does not require <see cref="Checkpoints" /> (the graph already owns its checkpointer).
    /// </summary>
    /// <param name="graph">Compiled graph instance.</param>
    /// <returns>This builder for chaining.</returns>
    public VolutaBuilder Graph(CompiledGraph graph)
    {
        graphFactory = _ => graph;
        graphRequiresCheckpointer = false;
        return this;
    }

    /// <summary>
    ///     Registers a graph factory that receives the resolved <see cref="ICheckpointer" />.
    ///     Requires exactly one <c>Checkpoints.Use*</c> call.
    /// </summary>
    /// <param name="factory">Builds and compiles the graph once per process.</param>
    /// <returns>This builder for chaining.</returns>
    public VolutaBuilder Graph(Func<IServiceProvider, ICheckpointer, CompiledGraph> factory)
    {
        graphFactory = serviceProvider =>
            factory(serviceProvider, serviceProvider.GetRequiredService<ICheckpointer>());
        graphRequiresCheckpointer = true;
        return this;
    }

    /// <summary>
    ///     Registers a graph factory with full service provider access.
    ///     Call <see cref="Checkpoints" /> first when the factory resolves <see cref="ICheckpointer" />.
    ///     Pass <c>new CompileOptions { Services = sp }</c> into <c>Compile</c> for DI nodes / MAF.
    /// </summary>
    /// <param name="factory">Builds and compiles the graph once per process.</param>
    /// <returns>This builder for chaining.</returns>
    public VolutaBuilder Graph(Func<IServiceProvider, CompiledGraph> factory)
    {
        graphFactory = factory;
        graphRequiresCheckpointer = false;
        return this;
    }

    /// <summary>
    ///     Validates configuration and applies DI registrations.
    /// </summary>
    internal void Complete()
    {
        if (graphRequiresCheckpointer && !Checkpoints.IsProviderConfigured)
        {
            throw new InvalidOperationException(
                "AddVoluta Graph(sp, checkpointer => …) requires Checkpoints.Use* (UseInMemory, UseFile, UseEntityFrameworkCore, UseS3).");
        }

        if (graphFactory is { } factory)
        {
            Services.RemoveAll<CompiledGraph>();
            Services.AddSingleton(factory);
        }
    }
}
