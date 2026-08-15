using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.File;
using Xunit;

namespace Voluta.Checkpoints.File.Unit;

public sealed class FileCheckpointerThreadDiscoveryShould
{
    [Fact(DisplayName = "Given empty root, when ListThreadIdsAsync, then returns empty")]
    public async Task ReturnEmptyWhenNoThreads()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-disc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpointer = new FileCheckpointer(root);

            var ids = await checkpointer.ListThreadIdsAsync();

            ids.ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "Given put threads then new FileCheckpointer on same root, when ListThreadIdsAsync, then returns all ids")]
    public async Task DiscoverThreadsAfterRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-disc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new FileCheckpointer(root);
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

            var reloaded = new FileCheckpointer(root);
            var ids = await reloaded.ListThreadIdsAsync();

            ids.ShouldBe(["thread-a", "thread-b"]);
            reloaded.ShouldBeAssignableTo<IThreadDiscovery>();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
