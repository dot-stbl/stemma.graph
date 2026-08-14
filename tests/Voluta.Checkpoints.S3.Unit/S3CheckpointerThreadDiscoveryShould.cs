using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.S3;
using Xunit;

namespace Voluta.Checkpoints.S3.Unit;

public sealed class S3CheckpointerThreadDiscoveryShould
{
    [Fact(DisplayName = "Given put threads under key prefix, when ListThreadIdsAsync, then returns sanitized ids ordered")]
    public async Task DiscoverThreadsFromCommonPrefixes()
    {
        var client = InMemoryAmazonS3.Create();
        var checkpointer = new S3Checkpointer(
            client,
            new S3CheckpointerOptions
            {
                BucketName = "voluta-test",
                KeyPrefix = "checkpoints",
            });

        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "thread-z",
                Step = 1,
                Status = GraphRunStatus.Running,
            });
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "thread-a",
                Step = 1,
                Status = GraphRunStatus.Done,
            });

        var ids = await checkpointer.ListThreadIdsAsync();

        ids.ShouldBe(["thread-a", "thread-z"]);
        checkpointer.ShouldBeAssignableTo<IThreadDiscovery>();
    }

    [Fact(DisplayName = "Given empty bucket, when ListThreadIdsAsync, then returns empty")]
    public async Task ReturnEmptyWhenNoObjects()
    {
        var client = InMemoryAmazonS3.Create();
        var checkpointer = new S3Checkpointer(
            client,
            new S3CheckpointerOptions
            {
                BucketName = "voluta-test",
                KeyPrefix = "runs",
            });

        var ids = await checkpointer.ListThreadIdsAsync();

        ids.ShouldBeEmpty();
    }
}
