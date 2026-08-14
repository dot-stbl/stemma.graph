using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.File;

/// <summary>
///     <c>UseFile</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="FileCheckpointer" /> as singleton <see cref="ICheckpointer" />.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <param name="rootDirectory">Root directory for per-thread checkpoint JSON files.</param>
    /// <returns>The builder for chaining.</returns>
    public static VolutaCheckpointBuilder UseFile(
        this VolutaCheckpointBuilder builder,
        string rootDirectory)
    {
        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.AddSingleton<ICheckpointer>(_ => new FileCheckpointer(rootDirectory));
        return builder;
    }
}
