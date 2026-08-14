using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.Postgres.Unit;

[Collection(nameof(PostgresCollection))]
public sealed class PostgresCheckpointerConformanceShould(PostgresFixture fixture)
{
    [Fact(DisplayName = "Postgres checkpointer passes shared conformance suite")]
    public async Task PassesConformance()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        var checkpointer = fixture.CreateCheckpointer();
        await CheckpointerConformance.RunAllAsync(checkpointer);
    }
}
