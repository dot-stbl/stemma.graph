using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.Redis;

/// <summary>
///     <c>UseRedis</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="RedisCheckpointer" /> as singleton <see cref="ICheckpointer" />.
    ///     Requires <see cref="IConnectionMultiplexer" /> already registered in DI.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <param name="configure">Key-prefix / database options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     services.AddSingleton(IConnectionMultiplexer)(_
    ///         =&gt; ConnectionMultiplexer.Connect("localhost:6379"));
    ///     services.AddVolutaCheckpoints(c =&gt; c.UseRedis(o =&gt;
    ///     {
    ///         o.KeyPrefix = "voluta:";
    ///     }));
    ///     </code>
    /// </example>
    public static VolutaCheckpointBuilder UseRedis(
        this VolutaCheckpointBuilder builder,
        Action<RedisCheckpointerOptions>? configure = null)
    {
        var options = new RedisCheckpointerOptions();
        configure?.Invoke(options);

        builder.MarkProviderConfigured();
        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.RemoveAll<RedisCheckpointerOptions>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ICheckpointer>(static serviceProvider =>
            new RedisCheckpointer(
                serviceProvider.GetRequiredService<IConnectionMultiplexer>(),
                serviceProvider.GetRequiredService<RedisCheckpointerOptions>()));
        return builder;
    }
}
