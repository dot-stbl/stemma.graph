using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoint;

namespace Voluta.DependencyInjection.Checkpoints;

/// <summary>
///     Core <c>Use*</c> extensions for <see cref="VolutaCheckpointBuilder" /> (InMemory).
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers the built-in <see cref="InMemoryCheckpointer" /> as singleton <see cref="ICheckpointer" />.
    /// </summary>
    /// <param name="builder">Checkpoint builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static VolutaCheckpointBuilder UseInMemory(this VolutaCheckpointBuilder builder)
    {
        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.AddSingleton<ICheckpointer, InMemoryCheckpointer>();
        return builder;
    }
}
