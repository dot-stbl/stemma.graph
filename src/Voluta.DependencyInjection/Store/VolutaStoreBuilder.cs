using Microsoft.Extensions.DependencyInjection;
using Voluta.Abstractions.Store;

namespace Voluta.DependencyInjection.Store;

/// <summary>
///     Fluent registration surface for a single <see cref="IVolutaStore" />.
///     Core adds <see cref="VolutaStoreBuilderExtensions.UseInMemory" />; durable providers may
///     add <c>Use*</c> later without changing this type.
/// </summary>
public sealed class VolutaStoreBuilder(IServiceCollection services)
{
    private bool providerConfigured;

    /// <summary>Service collection being configured.</summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    ///     Whether a <c>Use*</c> store provider has been selected for this builder.
    /// </summary>
    public bool IsProviderConfigured => providerConfigured;

    /// <summary>
    ///     Marks that a store provider was registered (exactly one expected per builder).
    /// </summary>
    public void MarkProviderConfigured()
    {
        if (providerConfigured)
        {
            throw new InvalidOperationException(
                "A store provider is already configured on this builder. Call exactly one Use* method.");
        }

        providerConfigured = true;
    }
}
