using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Store;
using Voluta.Store;

namespace Voluta.DependencyInjection.Store;

/// <summary>
///     Core <c>Use*</c> extensions for <see cref="VolutaStoreBuilder" /> (InMemory).
/// </summary>
public static class VolutaStoreBuilderExtensions
{
    /// <summary>
    ///     Registers the built-in <see cref="InMemoryVolutaStore" /> as singleton <see cref="IVolutaStore" />.
    /// </summary>
    /// <param name="builder">Store builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static VolutaStoreBuilder UseInMemory(this VolutaStoreBuilder builder)
    {
        builder.MarkProviderConfigured();
        builder.Services.RemoveAll<IVolutaStore>();
        builder.Services.AddSingleton<IVolutaStore, InMemoryVolutaStore>();
        return builder;
    }
}
