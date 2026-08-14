using Microsoft.Extensions.DependencyInjection;

namespace Voluta.DependencyInjection.Checkpoints;

/// <summary>
///     Fluent registration surface for a single <see cref="Abstractions.Checkpoint.ICheckpointer" />.
///     Provider packages add <c>Use*</c> extensions (EF, S3, File); core adds <see cref="VolutaCheckpointBuilderExtensions.UseInMemory" />.
/// </summary>
public sealed class VolutaCheckpointBuilder(IServiceCollection services)
{
    /// <summary>Service collection being configured.</summary>
    public IServiceCollection Services { get; } = services;
}
