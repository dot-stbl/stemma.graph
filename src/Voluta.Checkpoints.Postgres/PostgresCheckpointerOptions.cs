namespace Voluta.Checkpoints.Postgres;

/// <summary>
///     Configuration for <see cref="PostgresCheckpointer" />.
/// </summary>
public sealed class PostgresCheckpointerOptions
{
    /// <summary>Npgsql connection string (required).</summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    ///     Postgres schema for the checkpoints table. Default <c>public</c>.
    ///     Identifier must match <c>^[A-Za-z_][A-Za-z0-9_]*$</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    ///     Table name (without schema). Default <c>voluta_checkpoints</c>.
    ///     Identifier must match <c>^[A-Za-z_][A-Za-z0-9_]*$</c>.
    /// </summary>
    public string Table { get; set; } = "voluta_checkpoints";

    /// <summary>
    ///     When true (default), <see cref="PostgresCheckpointer" /> ensures the table exists
    ///     on first use via <c>CREATE TABLE IF NOT EXISTS</c>. Set false when ops apply
    ///     <c>Schema/voluta_checkpoints.sql</c> (or equivalent) out-of-band.
    /// </summary>
    public bool EnsureSchemaOnStartup { get; set; } = true;
}
