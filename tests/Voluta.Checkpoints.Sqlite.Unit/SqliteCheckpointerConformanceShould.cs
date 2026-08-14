using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.Sqlite.Unit;

public sealed class SqliteCheckpointerConformanceShould
{
    [Fact(DisplayName = "SQLite checkpointer passes shared conformance suite")]
    public async Task PassesConformance()
    {
        var path = Path.Combine(Path.GetTempPath(), "voluta-sqlite-cp-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var checkpointer = new SqliteCheckpointer(path);
            await CheckpointerConformance.RunAllAsync(checkpointer);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Windows may keep a short handle; temp cleanup is best-effort.
        }
    }
}
