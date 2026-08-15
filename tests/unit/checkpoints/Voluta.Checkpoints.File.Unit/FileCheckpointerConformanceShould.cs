using Voluta.Testing.Conformance;
using Xunit;

namespace Voluta.Checkpoints.File.Unit;

public sealed class FileCheckpointerConformanceShould
{
    [Fact(DisplayName = "File checkpointer passes shared conformance suite")]
    public async Task PassesConformance()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-cp-" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpointer = new FileCheckpointer(root);
            await CheckpointerConformance.RunAllAsync(checkpointer);
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
