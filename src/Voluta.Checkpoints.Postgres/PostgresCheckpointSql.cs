using System.Reflection;
using System.Text.RegularExpressions;

namespace Voluta.Checkpoints.Postgres;

/// <summary>
///     SQL fragments and schema bootstrap for the Postgres checkpointer.
/// </summary>
internal static class PostgresCheckpointSql
{
    private static readonly Regex SafeIdentifier = new(
        "^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string QualifyTable(PostgresCheckpointerOptions options)
    {
        return $"{QuoteIdentifier(options.Schema)}.{QuoteIdentifier(options.Table)}";
    }

    public static string QuoteIdentifier(string identifier)
    {
        return !SafeIdentifier.IsMatch(identifier)
            ? throw new ArgumentException(
                $"Invalid Postgres identifier '{identifier}'. Use letters, digits, underscore; must start with letter or underscore.",
                nameof(identifier))
            : "\"" + identifier + "\"";
    }

    public static string CreateTableIfNotExists(PostgresCheckpointerOptions options)
    {
        var table = QualifyTable(options);
        return $"""
                CREATE TABLE IF NOT EXISTS {table} (
                    thread_id   text        NOT NULL,
                    step        bigint      NOT NULL,
                    status      text        NOT NULL,
                    snapshot    jsonb       NOT NULL,
                    created_at  timestamptz NOT NULL DEFAULT now(),
                    PRIMARY KEY (thread_id, step)
                );
                CREATE INDEX IF NOT EXISTS {QuoteIdentifier("ix_" + options.Table + "_thread_step")}
                    ON {table} (thread_id, step DESC);
                """;
    }

    public static string Upsert(PostgresCheckpointerOptions options)
    {
        var table = QualifyTable(options);
        return $"""
                INSERT INTO {table} (thread_id, step, status, snapshot)
                VALUES (@thread_id, @step, @status, @snapshot::jsonb)
                ON CONFLICT (thread_id, step) DO UPDATE SET
                    status = EXCLUDED.status,
                    snapshot = EXCLUDED.snapshot;
                """;
    }

    public static string GetLatest(PostgresCheckpointerOptions options)
    {
        var table = QualifyTable(options);
        return $"""
                SELECT snapshot
                FROM {table}
                WHERE thread_id = @thread_id
                ORDER BY step DESC
                LIMIT 1;
                """;
    }

    public static string ListByThread(PostgresCheckpointerOptions options)
    {
        var table = QualifyTable(options);
        return $"""
                SELECT snapshot
                FROM {table}
                WHERE thread_id = @thread_id
                ORDER BY step ASC;
                """;
    }

    public static string ListThreadIds(PostgresCheckpointerOptions options)
    {
        var table = QualifyTable(options);
        return $"""
                SELECT DISTINCT thread_id
                FROM {table}
                ORDER BY thread_id;
                """;
    }

    public static string LoadEmbeddedSchemaScript()
    {
        var assembly = typeof(PostgresCheckpointSql).Assembly;
        const string resourceName = "Voluta.Checkpoints.Postgres.Schema.voluta_checkpoints.sql";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded schema resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
