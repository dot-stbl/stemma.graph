using Shouldly;
using Xunit;

namespace Voluta.Checkpoints.Postgres.Unit;

public sealed class PostgresCheckpointSqlShould
{
    [Fact(DisplayName = "Given default options, when QualifyTable, then returns quoted public.voluta_checkpoints")]
    public void QualifyDefaultTable()
    {
        var options = new PostgresCheckpointerOptions
        {
            ConnectionString = "Host=localhost;Database=voluta",
        };

        PostgresCheckpointSql.QualifyTable(options).ShouldBe("\"public\".\"voluta_checkpoints\"");
    }

    [Fact(DisplayName = "Given custom schema and table, when CreateTableIfNotExists, then SQL uses quoted identifiers")]
    public void CreateTableUsesQuotedIdentifiers()
    {
        var options = new PostgresCheckpointerOptions
        {
            ConnectionString = "Host=localhost;Database=voluta",
            Schema = "ops",
            Table = "checkpoints",
        };

        var sql = PostgresCheckpointSql.CreateTableIfNotExists(options);

        sql.ShouldContain("\"ops\".\"checkpoints\"");
        sql.ShouldContain("CREATE TABLE IF NOT EXISTS");
        sql.ShouldContain("jsonb");
    }

    [Theory(DisplayName = "Given unsafe identifier, when QuoteIdentifier, then throws")]
    [InlineData("bad-name")]
    [InlineData("drop table")]
    [InlineData("")]
    [InlineData("1starts")]
    public void RejectUnsafeIdentifiers(string identifier)
    {
        Should.Throw<ArgumentException>(() => PostgresCheckpointSql.QuoteIdentifier(identifier));
    }

    [Fact(DisplayName = "When LoadEmbeddedSchemaScript is called, then returns CREATE TABLE for voluta_checkpoints")]
    public void EmbeddedSchemaScriptPresent()
    {
        var script = PostgresCheckpointSql.LoadEmbeddedSchemaScript();

        script.ShouldContain("voluta_checkpoints");
        script.ShouldContain("CREATE TABLE IF NOT EXISTS");
        script.ShouldContain("jsonb");
    }
}
