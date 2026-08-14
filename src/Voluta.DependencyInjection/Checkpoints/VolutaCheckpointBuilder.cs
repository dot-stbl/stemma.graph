using Microsoft.Extensions.DependencyInjection;
using Voluta.Abstractions.Checkpoint;

namespace Voluta.DependencyInjection.Checkpoints;

/// <summary>
///     Fluent registration surface for a single <see cref="ICheckpointer" />.
///     Provider packages add <c>Use*</c> extensions (EF, S3, File); core adds
///     <see cref="VolutaCheckpointBuilderExtensions.UseInMemory" />.
/// </summary>
public sealed class VolutaCheckpointBuilder(IServiceCollection services)
{
    private bool providerConfigured;

    /// <summary>Service collection being configured.</summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    ///     Whether a <c>Use*</c> provider has been selected for this builder.
    /// </summary>
    public bool IsProviderConfigured => providerConfigured;

    /// <summary>
    ///     Marks that a checkpoint provider was registered (exactly one expected).
    /// </summary>
    public void MarkProviderConfigured()
    {
        if (providerConfigured)
        {
            throw new InvalidOperationException(
                "A checkpoint provider is already configured on this builder. Call exactly one Use* method.");
        }

        providerConfigured = true;
    }
}
