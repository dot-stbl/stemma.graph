using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluta.Abstractions.Checkpoint;
using Voluta.DependencyInjection;
using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.EntityFrameworkCore.Unit;

public sealed class EntityFrameworkCoreCheckpointerConformanceShould
{
    [Fact(DisplayName = "Given UseEntityFrameworkCore on host DbContext, when conformance runs, then all scenarios pass")]
    public async Task PassesConformanceViaBuilder()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<HostDbContext>(options => options.UseSqlite(connection));
        services.AddVolutaCheckpoints(static checkpoints => checkpoints.UseEntityFrameworkCore<HostDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using (var setup = await provider
                         .GetRequiredService<IDbContextFactory<HostDbContext>>()
                         .CreateDbContextAsync())
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var checkpointer = provider.GetRequiredService<ICheckpointer>();
        await CheckpointerConformance.RunAllAsync(checkpointer);
    }

    [Fact(DisplayName = "Given dedicated VolutaCheckpointDbContext, when conformance runs, then all scenarios pass")]
    public async Task PassesConformanceDedicatedContext()
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

    [Fact(DisplayName = "Given EF InMemory provider, when conformance runs, then all scenarios pass")]
    public async Task PassesConformanceOnEfInMemory()
    {
        var databaseName = $"voluta-checkpoints-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddDbContextFactory<HostDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddVolutaCheckpoints(static checkpoints => checkpoints.UseEntityFrameworkCore<HostDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using (var setup = await provider
                         .GetRequiredService<IDbContextFactory<HostDbContext>>()
                         .CreateDbContextAsync())
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var checkpointer = provider.GetRequiredService<ICheckpointer>();
        await CheckpointerConformance.RunAllAsync(checkpointer);
    }
}

/// <summary>
///     Host-style DbContext that embeds Voluta checkpoints via the interface + model helper.
/// </summary>
file sealed class HostDbContext(DbContextOptions<HostDbContext> options)
    : DbContext(options), IVolutaCheckpointDbContext
{
    public DbSet<CheckpointRecord> Checkpoints => Set<CheckpointRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVolutaCheckpointModel();
        base.OnModelCreating(modelBuilder);
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
