using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Voluta.Checkpoints.Postgres.Unit;

/// <summary>
///     Shared Postgres for integration-style unit tests.
///     Prefer env <c>VOLUTA_TEST_PG</c> (or <c>TEST_DB_CONNECTION</c>); else Testcontainers when Docker works.
///     When neither is available, tests that need the fixture skip.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string EnvConnection = "VOLUTA_TEST_PG";
    private const string EnvConnectionAlt = "TEST_DB_CONNECTION";

    private PostgreSqlContainer? container;
    private NpgsqlDataSource? dataSource;

    public string? ConnectionString { get; private set; }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(ConnectionString);

    public NpgsqlDataSource DataSource =>
        dataSource ?? throw new InvalidOperationException("Postgres fixture is not available.");

    public async Task InitializeAsync()
    {
        var env = Environment.GetEnvironmentVariable(EnvConnection)
            ?? Environment.GetEnvironmentVariable(EnvConnectionAlt);

        if (!string.IsNullOrWhiteSpace(env))
        {
            ConnectionString = env;
            dataSource = NpgsqlDataSource.Create(ConnectionString);
            return;
        }

        try
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithReuse(true)
                .Build();
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
            dataSource = NpgsqlDataSource.Create(ConnectionString);
        }
        catch (Exception)
        {
            ConnectionString = null;
            dataSource = null;
            if (container is not null)
            {
                await container.DisposeAsync();
                container = null;
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (dataSource is not null)
        {
            await dataSource.DisposeAsync();
            dataSource = null;
        }

        if (container is not null)
        {
            await container.DisposeAsync();
            container = null;
        }
    }

    public PostgresCheckpointer CreateCheckpointer()
    {
        return !IsAvailable
            ? throw new InvalidOperationException("Postgres fixture is not available.")
            : new PostgresCheckpointer(
                DataSource,
                new PostgresCheckpointerOptions
                {
                    ConnectionString = ConnectionString!,
                    EnsureSchemaOnStartup = true,
                });
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
