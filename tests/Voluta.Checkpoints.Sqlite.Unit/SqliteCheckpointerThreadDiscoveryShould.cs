using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Xunit;

namespace Voluta.Checkpoints.Sqlite.Unit;

public sealed class SqliteCheckpointerThreadDiscoveryShould
{
    [Fact(DisplayName = "Given empty database, when ListThreadIdsAsync, then returns empty")]
    public async Task ReturnEmptyWhenNoThreads()
    {
        var path = Path.Combine(Path.GetTempPath(), "voluta-sqlite-disc-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var checkpointer = new SqliteCheckpointer(path);

            var ids = await checkpointer.ListThreadIdsAsync();

            ids.ShouldBeEmpty();
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact(DisplayName = "Given put threads then new SqliteCheckpointer on same file, when ListThreadIdsAsync, then returns all ids")]
    public async Task DiscoverThreadsAfterRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), "voluta-sqlite-disc-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using (var writer = new SqliteCheckpointer(path))
            {
                await writer.PutAsync(
                    new CheckpointSnapshot
                    {
                        ThreadId = "thread-b",
                        Step = 1,
                        Status = GraphRunStatus.Running,
                    });
                await writer.PutAsync(
                    new CheckpointSnapshot
                    {
                        ThreadId = "thread-a",
                        Step = 2,
                        Status = GraphRunStatus.Interrupted,
                    });
            }

            await using var reloaded = new SqliteCheckpointer(path);
            var ids = await reloaded.ListThreadIdsAsync();

            ids.ShouldBe(["thread-a", "thread-b"]);
            _ = reloaded.ShouldBeAssignableTo<IThreadDiscovery>();
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
        }
    }
}
