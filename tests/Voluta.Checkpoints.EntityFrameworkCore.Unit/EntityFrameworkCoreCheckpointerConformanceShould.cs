using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.EntityFrameworkCore.Unit;

public sealed class EntityFrameworkCoreCheckpointerConformanceShould
{
    [Fact(DisplayName = "EF Core checkpointer passes shared conformance suite")]
    public async Task PassesConformance()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<VolutaCheckpointDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new VolutaCheckpointDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var factory = new SharedOptionsDbContextFactory(options);
        var checkpointer = new EntityFrameworkCoreCheckpointer(factory);
        await CheckpointerConformance.RunAllAsync(checkpointer);
    }
}

/// <summary>
///     Test factory that always creates a context against the shared SQLite options/connection.
/// </summary>
file sealed class SharedOptionsDbContextFactory(DbContextOptions<VolutaCheckpointDbContext> options)
    : IDbContextFactory<VolutaCheckpointDbContext>
{
    public VolutaCheckpointDbContext CreateDbContext()
    {
        return new VolutaCheckpointDbContext(options);
    }
}
