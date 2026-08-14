using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection.Checkpoints;

namespace Voluta.Checkpoints.Sqlite;

/// <summary>
///     <c>UseSqlite</c> registration for <see cref="VolutaCheckpointBuilder" />.
/// </summary>
public static class VolutaCheckpointBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="SqliteCheckpointer" /> as singleton <see cref="ICheckpointer" />.
    /// </summary>
    /// <param name="builder">Checkpoint builder from <c>AddVolutaCheckpoints</c>.</param>
    /// <param name="databasePath">Path to the SQLite database file (created if missing).</param>
    /// <returns>The builder for chaining.</returns>
    public static VolutaCheckpointBuilder UseSqlite(
        this VolutaCheckpointBuilder builder,
        string databasePath)
    {
        builder.MarkProviderConfigured();
        _ = builder.Services.RemoveAll<ICheckpointer>();
        _ = builder.Services.AddSingleton<ICheckpointer>(_ => new SqliteCheckpointer(databasePath));
        return builder;
    }
}
