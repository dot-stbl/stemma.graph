using Microsoft.Extensions.DependencyInjection;
using Voluta.DependencyInjection.Checkpoints;
using Voluta.Graph;

namespace Voluta.DependencyInjection;

/// <summary>
///     DI registration helpers for a compiled Voluta and checkpoint stores.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers an already-compiled graph as a singleton.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="graph">Compiled graph instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoluta(this IServiceCollection services, CompiledGraph graph)
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
    public static IServiceCollection AddVoluta(
        this IServiceCollection services,
        Func<IServiceProvider, CompiledGraph> factory)
    {
        services.AddSingleton(factory);
        return services;
    }

    /// <summary>
    ///     Configures the process-wide <see cref="Abstractions.Checkpoint.ICheckpointer" /> via a fluent builder.
    ///     Call exactly one <c>Use*</c> (InMemory / File / EF / S3) inside <paramref name="configure" />.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Provider selection (e.g. <c>c =&gt; c.UseInMemory()</c>).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no <c>Use*</c> was called, or more than one was called.
    /// </exception>
    /// <example>
    ///     <code>
    ///     services.AddVolutaCheckpoints(c => c.UseInMemory());
    ///     services.AddVolutaCheckpoints(c => c.UseFile("./.voluta/checkpoints"));
    ///     services.AddVolutaCheckpoints(c => c.UseEntityFrameworkCore&lt;AppDbContext&gt;());
    ///     services.AddVolutaCheckpoints(c => c.UseS3(o => { o.BucketName = "voluta"; }));
    ///     </code>
    /// </example>
    public static IServiceCollection AddVolutaCheckpoints(
        this IServiceCollection services,
        Action<VolutaCheckpointBuilder> configure)
    {
        var builder = new VolutaCheckpointBuilder(services);
        configure(builder);
        return builder.IsProviderConfigured
            ? services
            : throw new InvalidOperationException(
                "AddVolutaCheckpoints requires exactly one Use* provider (UseInMemory, UseFile, UseEntityFrameworkCore, UseS3).");
    }
}
