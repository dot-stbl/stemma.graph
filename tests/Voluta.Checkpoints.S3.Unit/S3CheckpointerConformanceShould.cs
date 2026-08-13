using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.S3.Unit;

public sealed class S3CheckpointerConformanceShould
{
    [Fact(DisplayName = "S3 checkpointer passes shared conformance suite")]
    public async Task PassesConformance()
    {
        var client = InMemoryAmazonS3.Create();
        var checkpointer = new S3Checkpointer(
            client,
            new S3CheckpointerOptions
            {
                BucketName = "voluta-test",
                KeyPrefix = "checkpoints",
            });

        await CheckpointerConformance.RunAllAsync(checkpointer);
    }
}
