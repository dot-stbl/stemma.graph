using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.S3;

/// <summary>
///     <c>UseS3</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="S3Checkpointer" /> as singleton <see cref="ICheckpointer" />.
    ///     Requires <see cref="IAmazonS3" /> already registered in DI.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <param name="configure">Bucket / key-prefix options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     services.AddSingleton&lt;IAmazonS3&gt;(_ =&gt; new AmazonS3Client(RegionEndpoint.EUCentral1));
    ///     services.AddVolutaCheckpoints(c =&gt; c.UseS3(o =&gt;
    ///     {
    ///         o.BucketName = "voluta";
    ///         o.KeyPrefix = "runs";
    ///     }));
    ///     </code>
    /// </example>
    public static VolutaCheckpointBuilder UseS3(
        this VolutaCheckpointBuilder builder,
        Action<S3CheckpointerOptions> configure)
    {
        var options = new S3CheckpointerOptions { BucketName = "" };
        configure(options);
        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            throw new ArgumentException("BucketName is required.", nameof(configure));
        }

        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.RemoveAll<S3CheckpointerOptions>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ICheckpointer>(static serviceProvider =>
            new S3Checkpointer(
                serviceProvider.GetRequiredService<IAmazonS3>(),
                serviceProvider.GetRequiredService<S3CheckpointerOptions>()));
        return builder;
    }
}
