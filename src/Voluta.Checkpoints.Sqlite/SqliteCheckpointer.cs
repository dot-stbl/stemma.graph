using System.Text.Json;
using Microsoft.Data.Sqlite;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Checkpoints.Sqlite.Wire;

namespace Voluta.Checkpoints.Sqlite;

/// <summary>
///     SQLite checkpointer: one database file, history as (thread_id, step) rows with JSON payload.
/// </summary>
/// <remarks>
///     Host registration: <c>v.Checkpoints.UseSqlite(databasePath)</c> (or
///     <c>AddVolutaCheckpoints(c =&gt; c.UseSqlite(...))</c>). Direct construction is
///     internal for conformance / unit tests only.
///     Channel values must be wire-format v1 allow-listed shapes (primitives, string,
///     lists/dictionaries of those, JsonElement). Unsupported types fail Put with
///     <c>checkpoint.unsupported_value_type</c>.
///     Implements <see cref="IThreadDiscovery" /> by selecting distinct thread ids from the table.
/// </remarks>
public sealed class SqliteCheckpointer : ICheckpointer, IThreadDiscovery, IAsyncDisposable, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string connectionString;
    private readonly SqliteConnection connection;
    private bool disposed;

    /// <summary>
    ///     Creates a SQLite checkpointer at <paramref name="databasePath" />.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file (created if missing).</param>
    internal SqliteCheckpointer(string databasePath)
    {
        connectionString = BuildConnectionString(databasePath);
        connection = new SqliteConnection(connectionString);
        connection.Open();
        EnsureSchema();
    }

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = SqliteCheckpointDocument.FromSnapshot(snapshot);
        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO voluta_checkpoints (thread_id, step, status, payload_json)
                VALUES ($threadId, $step, $status, $payload)
                ON CONFLICT(thread_id, step) DO UPDATE SET
                    status = excluded.status,
                    payload_json = excluded.payload_json;
                """;
            _ = command.Parameters.AddWithValue("$threadId", snapshot.ThreadId);
            _ = command.Parameters.AddWithValue("$step", snapshot.Step);
            _ = command.Parameters.AddWithValue("$status", snapshot.Status.ToString());
            _ = command.Parameters.AddWithValue("$payload", json);
            _ = await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointPutFailed,
                $"Failed to put checkpoint for thread '{snapshot.ThreadId}' step {snapshot.Step}.",
                exception);
        }
        finally
        {
            _ = gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT payload_json
                FROM voluta_checkpoints
                WHERE thread_id = $threadId
                ORDER BY step DESC
                LIMIT 1;
                """;
            _ = command.Parameters.AddWithValue("$threadId", threadId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null or DBNull)
            {
                return null;
            }

            var json = (string)result;
            var document = JsonSerializer.Deserialize<SqliteCheckpointDocument>(json, JsonSerializerOptions.Web);
            return document?.ToSnapshot();
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointGetFailed,
                $"Failed to get checkpoint for thread '{threadId}'.",
                exception);
        }
        finally
        {
            _ = gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT payload_json
                FROM voluta_checkpoints
                WHERE thread_id = $threadId
                ORDER BY step ASC;
                """;
            _ = command.Parameters.AddWithValue("$threadId", threadId);

            var list = new List<CheckpointSnapshot>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var json = reader.GetString(0);
                var document = JsonSerializer.Deserialize<SqliteCheckpointDocument>(json, JsonSerializerOptions.Web);
                if (document is not null)
                {
                    list.Add(document.ToSnapshot());
                }
            }

            return list;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointListFailed,
                $"Failed to list checkpoints for thread '{threadId}'.",
                exception);
        }
        finally
        {
            _ = gate.Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Returns distinct <c>thread_id</c> values ordered by ordinal string comparison.
    /// </remarks>
    public async Task<IReadOnlyList<string>> ListThreadIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT DISTINCT thread_id
                FROM voluta_checkpoints
                ORDER BY thread_id COLLATE BINARY;
                """;

            var ids = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetString(0));
            }

            return ids;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointListFailed,
                "Failed to list thread identifiers from the SQLite checkpoint store.",
                exception);
        }
        finally
        {
            _ = gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        connection.Dispose();
        gate.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await connection.DisposeAsync();
        gate.Dispose();
    }

    private void EnsureSchema()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS voluta_checkpoints (
                thread_id TEXT NOT NULL,
                step INTEGER NOT NULL,
                status TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                PRIMARY KEY (thread_id, step)
            );
            CREATE INDEX IF NOT EXISTS ix_voluta_checkpoints_thread_step
                ON voluta_checkpoints (thread_id, step);
            """;
        _ = command.ExecuteNonQuery();
    }

    private static string BuildConnectionString(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        var full = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        return new SqliteConnectionStringBuilder
        {
            DataSource = full,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }
}
