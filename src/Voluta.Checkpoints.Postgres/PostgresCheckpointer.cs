using System.Text.Json;
using Npgsql;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Checkpoints.Postgres.Wire;

namespace Voluta.Checkpoints.Postgres;

/// <summary>
///     Postgres-native checkpointer: one row per (thread, step) with JSONB snapshot.
/// </summary>
/// <remarks>
///     Table layout: <c>{schema}.{table}</c> with PK <c>(thread_id, step)</c> and
///     <c>snapshot jsonb</c> holding wire-format v1 documents.
///     Host registration: <c>v.Checkpoints.UsePostgres(configure)</c>.
///     Direct construction is internal for conformance / unit tests only.
///     Channel values must be wire-format v1 allow-listed shapes; unsupported types fail Put with
///     <c>checkpoint.unsupported_value_type</c>.
///     Implements <see cref="IThreadDiscovery" /> via <c>SELECT DISTINCT thread_id</c>.
/// </remarks>
public sealed class PostgresCheckpointer : ICheckpointer, IThreadDiscovery
{
    private readonly NpgsqlDataSource dataSource;
    private readonly PostgresCheckpointerOptions options;
    private readonly SemaphoreSlim schemaGate = new(1, 1);
    private int schemaReady;

    /// <summary>
    ///     Creates a Postgres checkpointer over <paramref name="dataSource" />.
    /// </summary>
    /// <param name="dataSource">Npgsql data source (owns connection pooling).</param>
    /// <param name="options">Schema / table / ensure options.</param>
    internal PostgresCheckpointer(NpgsqlDataSource dataSource, PostgresCheckpointerOptions options)
    {
        this.dataSource = dataSource;
        this.options = options;
        _ = PostgresCheckpointSql.QualifyTable(options);
    }

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var document = PostgresCheckpointDocument.FromSnapshot(snapshot);
        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

        try
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PostgresCheckpointSql.Upsert(options);
            command.Parameters.AddWithValue("thread_id", snapshot.ThreadId);
            command.Parameters.AddWithValue("step", snapshot.Step);
            command.Parameters.AddWithValue("status", snapshot.Status.ToString());
            command.Parameters.AddWithValue("snapshot", json);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointPutFailed,
                $"Failed to put checkpoint for thread '{snapshot.ThreadId}' step {snapshot.Step}.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PostgresCheckpointSql.GetLatest(options);
            command.Parameters.AddWithValue("thread_id", threadId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var json = reader.GetString(0);
            return DeserializeSnapshot(json);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                VolutaErrorCodes.CheckpointGetFailed,
                $"Failed to get checkpoint for thread '{threadId}'.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PostgresCheckpointSql.ListByThread(options);
            command.Parameters.AddWithValue("thread_id", threadId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<CheckpointSnapshot>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var json = reader.GetString(0);
                list.Add(
                    DeserializeSnapshot(json)
                    ?? throw new CheckpointStoreException(
                        VolutaErrorCodes.CheckpointCorruptPayload,
                        $"Postgres row for thread '{threadId}' could not be deserialized as a checkpoint."));
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
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Distinct <c>thread_id</c> values ordered ascending.
    /// </remarks>
    public async Task<IReadOnlyList<string>> ListThreadIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PostgresCheckpointSql.ListThreadIds(options);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var ids = new List<string>();
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
                "Failed to list thread identifiers from the Postgres checkpoint store.",
                exception);
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (!options.EnsureSchemaOnStartup || Volatile.Read(ref schemaReady) == 1)
        {
            return;
        }

        await schemaGate.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref schemaReady) == 1)
            {
                return;
            }

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PostgresCheckpointSql.CreateTableIfNotExists(options);
            await command.ExecuteNonQueryAsync(cancellationToken);
            Volatile.Write(ref schemaReady, 1);
        }
        finally
        {
            schemaGate.Release();
        }
    }

    private static CheckpointSnapshot? DeserializeSnapshot(string json)
    {
        var document = JsonSerializer.Deserialize<PostgresCheckpointDocument>(json, JsonSerializerOptions.Web);
        return document?.ToSnapshot();
    }
}
