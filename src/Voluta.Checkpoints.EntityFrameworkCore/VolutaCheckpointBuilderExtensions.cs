using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     <c>UseEntityFrameworkCore</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="EntityFrameworkCoreCheckpointer{TContext}" /> as singleton
    ///     <see cref="ICheckpointer" />. Requires <see cref="IDbContextFactory{TContext}" />
    ///     already registered (provider-agnostic — host chooses Npgsql/SqlServer/SQLite).
    /// </summary>
    /// <typeparam name="TContext">
    ///     Host DbContext that implements <see cref="IVolutaCheckpointDbContext" />
    ///     (and applies <see cref="VolutaCheckpointModelExtensions.ApplyVolutaCheckpointModel" />).
    /// </typeparam>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     services.AddDbContextFactory&lt;AppDbContext&gt;(o =&gt; o.UseNpgsql(cs));
    ///     services.AddVolutaCheckpoints(c =&gt; c.UseEntityFrameworkCore&lt;AppDbContext&gt;());
    ///
    ///     public sealed class AppDbContext : DbContext, IVolutaCheckpointDbContext
    ///     {
    ///         public DbSet&lt;CheckpointRecord&gt; Checkpoints =&gt; Set&lt;CheckpointRecord&gt;();
    ///         protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///         {
    ///             modelBuilder.ApplyVolutaCheckpointModel();
    ///         }
    ///     }
    ///     </code>
    /// </example>
    public static VolutaCheckpointBuilder UseEntityFrameworkCore<TContext>(
        this VolutaCheckpointBuilder builder)
        where TContext : DbContext, IVolutaCheckpointDbContext
    {
        builder.MarkProviderConfigured();
        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.AddSingleton<ICheckpointer>(static serviceProvider =>
            new EntityFrameworkCoreCheckpointer<TContext>(
                serviceProvider.GetRequiredService<IDbContextFactory<TContext>>()));
        return builder;
    }

    /// <summary>
    ///     Registers the dedicated <see cref="VolutaCheckpointDbContext" /> checkpointer.
    ///     Requires <see cref="IDbContextFactory{TContext}" /> for
    ///     <see cref="VolutaCheckpointDbContext" />.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <returns>The builder for chaining.</returns>
    public static VolutaCheckpointBuilder UseEntityFrameworkCore(this VolutaCheckpointBuilder builder)
    {
        return builder.UseEntityFrameworkCore<VolutaCheckpointDbContext>();
    }
}
