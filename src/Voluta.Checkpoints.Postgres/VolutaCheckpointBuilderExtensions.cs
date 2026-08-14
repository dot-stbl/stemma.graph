using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.Postgres;

/// <summary>
///     <c>UsePostgres</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="PostgresCheckpointer" /> as singleton <see cref="ICheckpointer" />.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <param name="configure">Connection string / schema / table options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     services.AddVolutaCheckpoints(c =&gt; c.UsePostgres(o =&gt;
    ///     {
    ///         o.ConnectionString = "Host=localhost;Database=voluta;Username=voluta;Password=…";
    ///     }));
    ///     </code>
    /// </example>
    public static VolutaCheckpointBuilder UsePostgres(
        this VolutaCheckpointBuilder builder,
        Action<PostgresCheckpointerOptions> configure)
    {
        var options = new PostgresCheckpointerOptions();
        configure(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("ConnectionString is required.", nameof(configure));
        }

        // Validate identifiers early (throws ArgumentException on bad schema/table).
        _ = PostgresCheckpointSql.QualifyTable(options);

        builder.MarkProviderConfigured();
        builder.Services.RemoveAll<ICheckpointer>();
        builder.Services.RemoveAll<PostgresCheckpointerOptions>();
        builder.Services.RemoveAll<NpgsqlDataSource>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(static serviceProvider =>
        {
            var checkpointerOptions = serviceProvider.GetRequiredService<PostgresCheckpointerOptions>();
            return NpgsqlDataSource.Create(checkpointerOptions.ConnectionString);
        });
        builder.Services.AddSingleton<ICheckpointer>(static serviceProvider =>
            new PostgresCheckpointer(
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                serviceProvider.GetRequiredService<PostgresCheckpointerOptions>()));
        return builder;
    }
}
